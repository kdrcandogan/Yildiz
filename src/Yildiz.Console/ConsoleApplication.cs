using Spectre.Console;
using Yildiz.Console.Rendering;
using Yildiz.Core.Commands;
using Yildiz.Core.Generation;
using Yildiz.Core.Models;
using Yildiz.Core.Pathfinding;
using Yildiz.Core.Simulation;

namespace Yildiz.Console;

public sealed class ConsoleApplication
{
    public const int MinSupportedForestSize = 1;
    public const int MaxSupportedForestSize = ForestGenerator.MaxAllocatableSize;
    public static readonly TimeSpan DefaultStepDelay = TimeSpan.FromMilliseconds(200);

    private const string OptionRetrySameForest = "1. Aynı ormanda yeni senaryo dene";
    private const string OptionCreateNewForest = "2. Yeni orman oluştur";
    private const string OptionExitApplication = "3. Uygulamadan çık";

    private readonly GuaranteedPathBuilder _pathBuilder;
    private readonly ForestGenerator _forestGenerator;
    private readonly ScenarioParser _scenarioParser;
    private readonly SimulationEngine _simulationEngine;
    private readonly ForestConsoleRenderer _renderer;
    private readonly IAnsiConsole _console;
    private readonly TextReader _inputReader;
    private readonly int? _fixedSeed;
    private readonly TimeSpan _stepDelay;
    private string? _bufferedInputLine;

    public ConsoleApplication(
        GuaranteedPathBuilder? pathBuilder = null,
        ForestGenerator? forestGenerator = null,
        ScenarioParser? scenarioParser = null,
        SimulationEngine? simulationEngine = null,
        ForestConsoleRenderer? renderer = null,
        IAnsiConsole? console = null,
        TextReader? inputReader = null,
        int? fixedSeed = null,
        TimeSpan? stepDelay = null)
    {
        _console = console ?? AnsiConsole.Console;
        _inputReader = inputReader ?? System.Console.In;
        _pathBuilder = pathBuilder ?? new GuaranteedPathBuilder();
        _forestGenerator = forestGenerator ?? new ForestGenerator(_pathBuilder);
        _scenarioParser = scenarioParser ?? new ScenarioParser();
        _simulationEngine = simulationEngine ?? new SimulationEngine();
        _renderer = renderer ?? new ForestConsoleRenderer(_console);
        _fixedSeed = fixedSeed;
        _stepDelay = stepDelay ?? DefaultStepDelay;
    }

    public void Run()
    {
        _renderer.RenderWelcomeHeader();

        bool createNewForest = true;
        bool clearBeforeNewForest = false;
        bool clearBeforeRetrySameForest = false;
        ForestGenerationResult? currentGeneration = null;

        while (true)
        {
            if (createNewForest || currentGeneration is null)
            {
                if (clearBeforeNewForest)
                {
                    _renderer.ClearScreen();
                    _renderer.RenderWelcomeHeader();
                    clearBeforeNewForest = false;
                }

                int? forestSize = PromptForestSize();
                if (!forestSize.HasValue)
                {
                    return;
                }

                try
                {
                    currentGeneration = _forestGenerator.Generate(forestSize.Value, _fixedSeed);
                }
                catch (Exception ex) when (ex is OutOfMemoryException or OverflowException or ArgumentOutOfRangeException)
                {
                    _renderer.RenderErrorMessage(
                        $"Seçilen {forestSize.Value}×{forestSize.Value} orman boyutu için yeterli bellek ayrılamadı veya sayısal sınır aşıldı. " +
                        "Lütfen daha küçük bir orman boyutu giriniz.");
                    continue;
                }

                createNewForest = false;
            }
            else if (clearBeforeRetrySameForest)
            {
                _renderer.ClearScreen();
                clearBeforeRetrySameForest = false;
            }

            ForestBoard currentBoard = currentGeneration.Board;
            _renderer.RenderInitialForest(currentBoard);

            if (currentBoard.IsHole(currentBoard.StartPosition))
            {
                SimulationResult instantSuccess = _simulationEngine.Run(currentBoard, []);
                _renderer.RenderFinalSummary(currentBoard, instantSuccess, totalRequestedCommands: 0);
            }
            else
            {
                _renderer.RenderCommandGuide();

                IReadOnlyList<ScenarioCommand>? commands = PromptValidScenario();
                if (commands is null)
                {
                    return;
                }

                try
                {
                    SimulationResult result = _simulationEngine.Run(currentBoard, commands);
                    _renderer.RenderSimulation(currentBoard, result, commands.Count, _stepDelay);

                    if (ShouldOfferSampleSolution(result.Status))
                    {
                        HandleSampleSolutionOffer(currentGeneration);
                    }
                }
                catch (OutOfMemoryException)
                {
                    _renderer.RenderErrorMessage(
                        "Simülasyon adımları işlenirken bellek sınırı aşıldı. Lütfen daha kısa bir senaryo veya daha küçük bir orman boyutu deneyiniz.");
                }
            }

            PostScenarioAction nextAction = PromptPostScenarioAction();
            switch (nextAction)
            {
                case PostScenarioAction.RetrySameForest:
                    createNewForest = false;
                    clearBeforeRetrySameForest = true;
                    break;

                case PostScenarioAction.CreateNewForest:
                    createNewForest = true;
                    clearBeforeNewForest = true;
                    break;

                case PostScenarioAction.Exit:
                default:
                    _console.MarkupLine("[bold deepskyblue1]Yıldız Orman Macerası'na katıldığınız için teşekkür ederiz.[/]");
                    return;
            }
        }
    }

    public static bool ShouldOfferSampleSolution(SimulationStatus status) =>
        status is SimulationStatus.Dead
            or SimulationStatus.Blocked
            or SimulationStatus.OutOfBounds
            or SimulationStatus.Incomplete;

    public static bool TryValidateForestSize(string? rawInput, out int size, out string errorMessage)
    {
        size = 0;

        if (string.IsNullOrWhiteSpace(rawInput))
        {
            errorMessage = "Orman boyutu boş bırakılamaz. Lütfen pozitif bir tam sayı giriniz (örnek: 3, 7, 16).";
            return false;
        }

        string trimmed = rawInput.Trim().Trim('\uFEFF').Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            errorMessage = "Orman boyutu boş bırakılamaz. Lütfen pozitif bir tam sayı giriniz (örnek: 3, 7, 16).";
            return false;
        }

        if (!int.TryParse(trimmed, out int parsedSize))
        {
            if (IsSignedIntegerLiteral(trimmed))
            {
                errorMessage = trimmed.StartsWith('-')
                    ? "Orman boyutu pozitif bir tam sayı olmalıdır (en az 1)."
                    : $"Girilen orman boyutu 32-bit tam sayı sınırını ({int.MaxValue}) aşıyor. " +
                      $"Lütfen {MinSupportedForestSize} ile {MaxSupportedForestSize} arasında pozitif bir tam sayı giriniz.";
                return false;
            }

            errorMessage = "Geçersiz giriş. Lütfen yalnızca tam sayı değeri giriniz (örnek: 3, 7, 16).";
            return false;
        }

        if (parsedSize < MinSupportedForestSize)
        {
            errorMessage = "Orman boyutu pozitif bir tam sayı olmalıdır (en az 1).";
            return false;
        }

        if (parsedSize > MaxSupportedForestSize)
        {
            errorMessage =
                $"Girilen {parsedSize}×{parsedSize} orman boyutu toplam hücre sayısında 32-bit sayısal taşmaya ve bellek sınırının aşılmasına neden olmaktadır " +
                $"(en fazla {MaxSupportedForestSize}×{MaxSupportedForestSize}). Lütfen {MinSupportedForestSize} ile {MaxSupportedForestSize} arasında bir değer giriniz.";
            return false;
        }

        size = parsedSize;
        errorMessage = string.Empty;
        return true;
    }

    private static bool IsSignedIntegerLiteral(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        int startIndex = value[0] is '+' or '-' ? 1 : 0;
        if (startIndex >= value.Length)
        {
            return false;
        }

        for (int i = startIndex; i < value.Length; i++)
        {
            if (!char.IsAsciiDigit(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void HandleSampleSolutionOffer(ForestGenerationResult generation)
    {
        ForestBoard board = generation.Board;
        IReadOnlyList<ScenarioCommand> sampleCommands = _pathBuilder.BuildCommands(
            generation.GuaranteedPath,
            board.InitialDirection);

        _renderer.RenderSampleSolutionOffer(sampleCommands);

        if (!PromptPlaySampleSolution())
        {
            return;
        }

        PlaySampleSolution(board, sampleCommands);
    }

    private void PlaySampleSolution(ForestBoard board, IReadOnlyList<ScenarioCommand> sampleCommands)
    {
        _renderer.ClearScreen();
        _renderer.RenderInitialForest(board);
        _renderer.RenderSampleSolutionPlaybackHeader(sampleCommands);

        SimulationResult sampleResult = _simulationEngine.Run(board, sampleCommands);

        if (sampleResult.Status != SimulationStatus.Success)
        {
            _renderer.RenderErrorMessage(
                $"Örnek çözüm doğrulaması başarısız oldu: Beklenen durum 'Success' iken '{sampleResult.Status}' elde edildi.");
        }

        _renderer.RenderSimulation(board, sampleResult, sampleCommands.Count, _stepDelay);
    }

    private bool PromptPlaySampleSolution()
    {
        while (true)
        {
            _console.Markup("[bold]Örnek çözümün oynatılmasını ister misiniz? (E/H): [/]");
            string? input = ReadLineFromInput();

            if (input is null)
            {
                _console.WriteLine();
                return false;
            }

            string normalized = input.Trim();

            if (IsYesResponse(normalized))
            {
                _console.WriteLine();
                return true;
            }

            if (IsNoResponse(normalized))
            {
                _console.WriteLine();
                return false;
            }

            if (normalized is "1" or "2" or "3")
            {
                _bufferedInputLine = normalized;
                _console.WriteLine();
                return false;
            }

            _renderer.RenderErrorMessage("Geçersiz giriş. Lütfen örnek çözümü oynatmak için 'E', geçmek için 'H' giriniz.");
        }
    }

    private static bool IsYesResponse(string value) =>
        value.Equals("E", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("EVET", StringComparison.OrdinalIgnoreCase);

    private static bool IsNoResponse(string value) =>
        value.Equals("H", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("HAYIR", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("HAYİR", StringComparison.OrdinalIgnoreCase);

    private int? PromptForestSize()
    {
        while (true)
        {
            _console.Markup("[bold]Orman boyutunu giriniz (örnek: 3, 7, 16): [/]");
            string? input = ReadLineFromInput();

            if (input is null)
            {
                return null;
            }

            if (TryValidateForestSize(input, out int size, out string errorMessage))
            {
                _console.WriteLine();
                return size;
            }

            _renderer.RenderErrorMessage(errorMessage);
        }
    }

    private IReadOnlyList<ScenarioCommand>? PromptValidScenario()
    {
        while (true)
        {
            _console.Markup("[bold]Komut dizisini virgülle ayırarak giriniz (örnek: N,N,L,N,N): [/]");
            string? input = ReadLineFromInput();

            if (input is null)
            {
                return null;
            }

            ScenarioParseResult parseResult = _scenarioParser.Parse(input);
            if (parseResult.IsSuccess)
            {
                _console.WriteLine();
                return parseResult.Commands;
            }

            string localizedError = _renderer.FormatParseError(parseResult);
            _renderer.RenderErrorMessage(localizedError);
        }
    }

    private PostScenarioAction PromptPostScenarioAction()
    {
        bool useInteractiveMenu =
            _bufferedInputLine is null &&
            !System.Console.IsInputRedirected &&
            !System.Console.IsOutputRedirected &&
            _console.Profile.Capabilities.Interactive;

        if (useInteractiveMenu)
        {
            string selected = _console.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold deepskyblue1]Ne yapmak istersiniz?[/]")
                    .AddChoices(
                        OptionRetrySameForest,
                        OptionCreateNewForest,
                        OptionExitApplication));

            return MapSelectionToAction(selected);
        }

        while (true)
        {
            _console.MarkupLine("[bold deepskyblue1]Ne yapmak istersiniz?[/]");
            _console.MarkupLine($"  {Markup.Escape(OptionRetrySameForest)}");
            _console.MarkupLine($"  {Markup.Escape(OptionCreateNewForest)}");
            _console.MarkupLine($"  {Markup.Escape(OptionExitApplication)}");
            _console.Markup("[bold]Seçiminiz (1, 2 veya 3): [/]");

            string? choice = ReadLineFromInput();
            if (choice is null)
            {
                return PostScenarioAction.Exit;
            }

            switch (choice.Trim())
            {
                case "1":
                    _console.WriteLine();
                    return PostScenarioAction.RetrySameForest;

                case "2":
                    _console.WriteLine();
                    return PostScenarioAction.CreateNewForest;

                case "3":
                    _console.WriteLine();
                    return PostScenarioAction.Exit;

                default:
                    _renderer.RenderErrorMessage("Geçersiz seçim. Lütfen 1, 2 veya 3 değerlerinden birini giriniz.");
                    break;
            }
        }
    }

    private static PostScenarioAction MapSelectionToAction(string selectedOption) => selectedOption switch
    {
        OptionRetrySameForest => PostScenarioAction.RetrySameForest,
        OptionCreateNewForest => PostScenarioAction.CreateNewForest,
        _ => PostScenarioAction.Exit
    };

    private string? ReadLineFromInput()
    {
        if (_bufferedInputLine is not null)
        {
            string buffered = _bufferedInputLine;
            _bufferedInputLine = null;
            return buffered;
        }

        string? line = _inputReader.ReadLine();
        return line?.TrimStart('\uFEFF');
    }

    private enum PostScenarioAction
    {
        RetrySameForest = 1,
        CreateNewForest = 2,
        Exit = 3
    }
}
