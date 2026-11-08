using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using Yildiz.Core.Commands;
using Yildiz.Core.Models;
using Yildiz.Core.Simulation;

namespace Yildiz.Console.Rendering;

public sealed class ForestConsoleRenderer
{
    private const int CompactPaddingThreshold = 10;
    private const int MaxTableBoardSize = 16;
    private const int MaxSpacedCompactGridSize = 24;
    private const int MaxFullCompactGridSize = 50;
    private const int MinLiveViewportRows = 10;
    private const int LiveFrameOverheadRows = 10;
    private const int MinimumColumnsPerCell = 4;
    private const int TableFrameOverheadColumns = 6;
    private const int DefaultCommandWrapWidth = 68;
    private const int MinCommandWrapWidth = 30;

    private readonly IAnsiConsole _console;

    public ForestConsoleRenderer(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    public void ClearScreen()
    {
        if (System.Console.IsOutputRedirected ||
            !_console.Profile.Capabilities.Ansi ||
            !_console.Profile.Capabilities.Interactive)
        {
            return;
        }

        try
        {
            _console.Clear();
        }
        catch (IOException)
        {
        }
    }

    public void RenderWelcomeHeader()
    {
        var headerContent = new Rows(
            new Markup("[bold deepskyblue1]YILDIZ[/]"),
            new Markup("[bold white]ORMAN MACERASI[/]"),
            new Text("Tavşan Yıldız'ı ormandaki yuvasına ulaştırın."));

        var panel = new Panel(headerContent)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.DeepSkyBlue1),
            Padding = new Padding(2, 0, 2, 0)
        };

        _console.Write(panel);
        _console.WriteLine();
    }

    public void RenderInitialForest(ForestBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        RabbitState initialState = RabbitState.CreateInitial(board.StartPosition, board.InitialDirection);
        Position[] initialVisited = [board.StartPosition];
        SimulationStatus initialStatus = board.IsHole(board.StartPosition)
            ? SimulationStatus.Success
            : SimulationStatus.Running;

        IRenderable frame = BuildSimulationFrame(
            board,
            initialState,
            initialVisited,
            stepNumber: 0,
            totalSteps: 0,
            executedCommand: null,
            initialStatus);

        _console.Write(frame);
        _console.WriteLine();
    }

    public void RenderCommandGuide()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold deepskyblue1]Komut Rehberi ve Kurallar[/]")
            .AddColumn(new TableColumn("[bold]Komut[/]").Centered())
            .AddColumn(new TableColumn("[bold]Açıklama[/]"));

        table.AddRow("[bold cyan]N[/]", "İleri (tavşanın baktığı yönde 1 hücre ilerler)");
        table.AddRow("[bold cyan]P[/]", "Geri (tavşanın baktığı yönün tersine 1 hücre gider; yön değişmez)");
        table.AddRow("[bold cyan]R[/]", "Sağa dön (bulunduğu hücrede 90° saat yönünde döner)");
        table.AddRow("[bold cyan]L[/]", "Sola dön (bulunduğu hücrede 90° saat yönünün tersine döner)");
        table.AddRow("[bold yellow]J[/]", "Zıplama hazırlığı ([bold]J,N[/] veya [bold]J,P[/] ile sonraki hücredeki çiti geçer)");
        table.AddRow("[bold yellow]İ[/]", "Eğilme hazırlığı ([bold]İ,N[/] veya [bold]İ,P[/] ile sonraki hücredeki dikenli teli geçer)");

        var rules = new Rows(
            table,
            new Markup("[grey]• J ve İ tek başına hareket sağlamaz; hemen ardından N veya P gelmelidir.[/]"),
            new Markup("[grey]• Tüm komutları virgülle ayırarak tek satırda giriniz. Örnek:[/] [bold green]N,N,L,N,N[/]"));

        _console.Write(rules);
        _console.WriteLine();
    }

    public void RenderSampleSolutionOffer(IReadOnlyList<ScenarioCommand> sampleCommands)
    {
        ArgumentNullException.ThrowIfNull(sampleCommands);

        int wrapWidth = GetCommandWrapWidth();
        string formattedCommands = FormatCommandSequence(sampleCommands, wrapWidth);

        var content = new Rows(
            new Markup("Aynı orman üzerinde hedefe ulaşan geçerli bir senaryo bulunmaktadır."),
            new Text(string.Empty),
            new Markup($"[bold]Örnek Komut Dizisi ({sampleCommands.Count} komut):[/]"),
            new Markup($"[bold green]{Markup.Escape(formattedCommands)}[/]"));

        var panel = new Panel(content)
        {
            Header = new PanelHeader("[bold deepskyblue1] ÖRNEK BAŞARILI ÇÖZÜM [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.DeepSkyBlue1),
            Padding = new Padding(1, 0, 1, 0)
        };

        _console.Write(panel);
        _console.WriteLine();
    }

    public void RenderSampleSolutionPlaybackHeader(IReadOnlyList<ScenarioCommand> sampleCommands)
    {
        ArgumentNullException.ThrowIfNull(sampleCommands);

        int wrapWidth = GetCommandWrapWidth();
        string formattedCommands = FormatCommandSequence(sampleCommands, wrapWidth);

        var content = new Rows(
            new Markup("[bold deepskyblue1]ÖRNEK BAŞARILI ÇÖZÜM OYNATILIYOR[/]"),
            new Text(string.Empty),
            new Markup($"[bold]Oynatılan Komut Dizisi ({sampleCommands.Count} komut):[/]"),
            new Markup($"[bold green]{Markup.Escape(formattedCommands)}[/]"));

        var panel = new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.DeepSkyBlue1),
            Padding = new Padding(1, 0, 1, 0)
        };

        _console.Write(panel);
        _console.WriteLine();
    }

    public void RenderErrorMessage(string message)
    {
        var panel = new Panel(new Markup($"[bold red]{Markup.Escape(message)}[/]"))
        {
            Header = new PanelHeader("[bold red] Girdi Hatası [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Red),
            Padding = new Padding(1, 0, 1, 0)
        };

        _console.Write(panel);
        _console.WriteLine();
    }

    public string FormatParseError(ScenarioParseResult parseResult)
    {
        ArgumentNullException.ThrowIfNull(parseResult);

        if (parseResult.ErrorCommandNumber is null)
        {
            return "Komut dizisi boş bırakılamaz. Lütfen virgülle ayrılmış geçerli bir komut dizisi giriniz (örnek: N,N,L,N,N).";
        }

        int commandNumber = parseResult.ErrorCommandNumber.Value;
        string token = parseResult.InvalidToken ?? string.Empty;

        if (string.IsNullOrEmpty(token))
        {
            return $"Hatalı komut dizisi: {commandNumber}. sıradaki komut boş bırakılmış. Lütfen ardışık virgülleri kontrol ediniz.";
        }

        if (parseResult.ErrorMessage?.Contains("cannot be the last command", StringComparison.Ordinal) == true)
        {
            return $"Hatalı komut dizisi: {commandNumber}. sıradaki '{token}' hazırlık komutu senaryonun sonunda tek başına kullanılamaz. 'J' veya 'İ' komutundan hemen sonra 'N' veya 'P' gelmelidir.";
        }

        if (parseResult.ErrorMessage?.Contains("is invalid after command", StringComparison.Ordinal) == true)
        {
            return $"Hatalı komut dizisi: {commandNumber - 1}. sıradaki hazırlık komutundan hemen sonra {commandNumber}. sırada '{token}' komutu kullanılamaz. 'J' veya 'İ' komutundan hemen sonra 'N' veya 'P' gelmelidir.";
        }

        return $"Hatalı komut dizisi: {commandNumber}. sıradaki '{token}' geçerli bir komut değildir. Geçerli komutlar: N, P, R, L, J, İ.";
    }

    public void RenderSimulation(
        ForestBoard board,
        SimulationResult result,
        int totalRequestedCommands,
        TimeSpan stepDelay)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(result);

        bool canAnimateLive =
            !System.Console.IsOutputRedirected &&
            _console.Profile.Capabilities.Ansi &&
            _console.Profile.Capabilities.Interactive &&
            result.Steps.Count > 0;

        if (canAnimateLive)
        {
            RabbitState initialState = RabbitState.CreateInitial(board.StartPosition, board.InitialDirection);
            IRenderable initialFrame = BuildSimulationFrame(
                board,
                initialState,
                [board.StartPosition],
                stepNumber: 0,
                totalSteps: totalRequestedCommands,
                executedCommand: null,
                SimulationStatus.Running,
                constrainHeightToTerminal: true);

            _console.Live(initialFrame)
                .AutoClear(false)
                .Start(context =>
                {
                    foreach (SimulationStep step in result.Steps)
                    {
                        if (stepDelay > TimeSpan.Zero)
                        {
                            Thread.Sleep(stepDelay);
                        }

                        IRenderable stepFrame = BuildSimulationFrame(
                            board,
                            step.RabbitState,
                            step.VisitedPositions,
                            step.StepNumber,
                            totalRequestedCommands,
                            step.Command,
                            step.Status,
                            constrainHeightToTerminal: true);

                        context.UpdateTarget(stepFrame);
                        context.Refresh();
                    }
                });

            _console.WriteLine();
        }
        else
        {
            RenderStaticStepProgress(board, result, totalRequestedCommands);
        }

        RenderFinalSummary(board, result, totalRequestedCommands);
    }

    public void RenderFinalSummary(
        ForestBoard board,
        SimulationResult result,
        int totalRequestedCommands)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(result);

        (string headline, Color borderColor, string? extraDetail) = GetFinalStatusPresentation(board, result);

        bool isAlive = result.FinalState.IsAlive && result.Status != SimulationStatus.Dead;
        string statusText = FormatStatusText(result.Status);
        string finalPositionText = FormatOneBasedPosition(result.FinalPosition);
        string finalDirectionText = FormatDirectionText(result.FinalDirection, isAlive);
        string executedCountText = $"{result.Steps.Count} / {totalRequestedCommands}";

        List<IRenderable> summaryLines =
        [
            new Markup($"[bold {borderColor.ToMarkup()}]{Markup.Escape(headline)}[/]")
        ];

        if (!string.IsNullOrWhiteSpace(extraDetail))
        {
            summaryLines.Add(new Markup($"[bold]{Markup.Escape(extraDetail)}[/]"));
        }

        summaryLines.Add(new Text(string.Empty));
        summaryLines.Add(new Markup(
            $"[bold]Oyun Durumu:[/] {statusText}   " +
            $"[bold]Son Konum:[/] {finalPositionText}   " +
            $"[bold]Son Yön:[/] {finalDirectionText}   " +
            $"[bold]İşlenen Komut Sayısı:[/] {executedCountText}"));

        var summaryPanel = new Panel(new Rows(summaryLines))
        {
            Header = new PanelHeader($"[bold {borderColor.ToMarkup()}] Simülasyon Sonucu [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(borderColor),
            Padding = new Padding(1, 0, 1, 0)
        };

        _console.Write(summaryPanel);
        _console.WriteLine();
    }

    public static string GetRabbitSymbol(Direction direction, bool isAlive = true)
    {
        char prefix = isAlive ? 'Y' : 'X';
        char arrow = direction switch
        {
            Direction.South => '↓',
            Direction.North => '↑',
            Direction.East => '→',
            Direction.West => '←',
            _ => '↓'
        };

        return $"{prefix}{arrow}";
    }

    public static string FormatCommandSequence(
        IReadOnlyList<ScenarioCommand> commands,
        int maxLineLength = DefaultCommandWrapWidth)
    {
        ArgumentNullException.ThrowIfNull(commands);

        if (commands.Count == 0)
        {
            return string.Empty;
        }

        int effectiveMaxLength = Math.Max(MinCommandWrapWidth, maxLineLength);
        StringBuilder builder = new();
        int currentLineLength = 0;

        for (int index = 0; index < commands.Count; index++)
        {
            string token = FormatCommandShort(commands[index]);
            bool isLast = index == commands.Count - 1;
            string segment = isLast ? token : $"{token},";

            if (currentLineLength > 0 && currentLineLength + segment.Length > effectiveMaxLength)
            {
                builder.AppendLine();
                currentLineLength = 0;
            }

            builder.Append(segment);
            currentLineLength += segment.Length;
        }

        return builder.ToString();
    }

    private int GetCommandWrapWidth()
    {
        int profileWidth = _console.Profile.Width;
        if (profileWidth <= 0)
        {
            return DefaultCommandWrapWidth;
        }

        return Math.Max(MinCommandWrapWidth, profileWidth - 10);
    }

    private void RenderStaticStepProgress(
        ForestBoard board,
        SimulationResult result,
        int totalRequestedCommands)
    {
        foreach (SimulationStep step in result.Steps)
        {
            bool isAlive = step.IsAlive && step.Status != SimulationStatus.Dead;
            string line =
                $"Adım {step.StepNumber}/{totalRequestedCommands}: " +
                $"Komut={FormatCommandShort(step.Command)} | " +
                $"Konum={FormatOneBasedPosition(step.Position)} | " +
                $"Yön={FormatDirectionPlainText(step.Direction, isAlive)} | " +
                $"Geçiş Hazırlığı={FormatBypassPlainText(step.PendingBypass)} | " +
                $"Durum={FormatStatusPlainText(step.Status)}";

            _console.MarkupLine($"[grey]{Markup.Escape(line)}[/]");
        }

        ScenarioCommand? lastCommand = result.Steps.Count > 0
            ? result.Steps[^1].Command
            : null;

        IRenderable finalFrame = BuildSimulationFrame(
            board,
            result.FinalState,
            result.VisitedPositions,
            result.Steps.Count,
            totalRequestedCommands,
            lastCommand,
            result.Status);

        _console.Write(finalFrame);
        _console.WriteLine();
    }

    private IRenderable BuildSimulationFrame(
        ForestBoard board,
        RabbitState rabbitState,
        IReadOnlyCollection<Position> visitedPositions,
        int stepNumber,
        int totalSteps,
        ScenarioCommand? executedCommand,
        SimulationStatus status,
        bool constrainHeightToTerminal = false)
    {
        HashSet<Position> visitedSet = new(visitedPositions);

        IRenderable boardGrid = ShouldUseCompactTextGrid(board.Size)
            ? BuildCompactTextGrid(board, rabbitState, visitedSet, status, constrainHeightToTerminal)
            : BuildBoardTable(board, rabbitState, visitedSet, status);

        bool isAlive = rabbitState.IsAlive && status != SimulationStatus.Dead;
        string commandText = executedCommand.HasValue
            ? FormatCommandText(executedCommand.Value)
            : "Henüz komut çalıştırılmadı";

        var statusPanel = new Panel(
            new Rows(
                new Markup(
                    $"[bold]Orman:[/] {board.Size}×{board.Size}   " +
                    $"[bold]Adım:[/] {stepNumber} / {totalSteps}   " +
                    $"[bold]Komut:[/] {commandText}"),
                new Markup(
                    $"[bold]Konum:[/] {FormatOneBasedPosition(rabbitState.Position)}   " +
                    $"[bold]Yön:[/] {FormatDirectionText(rabbitState.Direction, isAlive)}   " +
                    $"[bold]Geçiş Hazırlığı:[/] {FormatBypassText(rabbitState.PendingBypass)}   " +
                    $"[bold]Durum:[/] {FormatStatusText(status)}"),
                new Markup(
                    "[grey]Semboller:[/] " +
                    "[bold deepskyblue1]Y↓/Y↑/Y→/Y←[/]: Tavşan  " +
                    "[bold red]X↓/X↑/X→/X←[/]: Ölü Tavşan  " +
                    "[bold green]H[/]: Tavşan Deliği  " +
                    "[bold red]W[/]: Kurt  " +
                    "[bold indianred1]F[/]: Tilki  " +
                    "[bold yellow]T[/]: Dikenli Tel  " +
                    "[bold darkorange]C[/]: Çit  " +
                    "[teal]•[/]: Geçilmiş Boş Hücre  " +
                    "[grey]·[/]: Boş Hücre")))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey),
            Padding = new Padding(1, 0, 1, 0)
        };

        return new Rows(boardGrid, statusPanel);
    }

    private bool ShouldUseCompactTextGrid(int size)
    {
        if (size > MaxTableBoardSize)
        {
            return true;
        }

        int terminalWidth = _console.Profile.Width;
        if (terminalWidth <= 0)
        {
            return false;
        }

        int estimatedRequiredWidth = ((size + 1) * MinimumColumnsPerCell) + TableFrameOverheadColumns;
        return terminalWidth < estimatedRequiredWidth;
    }

    private static Table BuildBoardTable(
        ForestBoard board,
        RabbitState rabbitState,
        HashSet<Position> visitedSet,
        SimulationStatus status)
    {
        bool useCompactPadding = board.Size > CompactPaddingThreshold;
        Padding cellPadding = useCompactPadding
            ? new Padding(0, 0, 1, 0)
            : new Padding(1, 0, 1, 0);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        table.AddColumn(new TableColumn("[bold grey]#[/]").RightAligned().Padding(cellPadding));

        for (int column = 1; column <= board.Size; column++)
        {
            table.AddColumn(new TableColumn($"[bold grey]{column}[/]").Centered().Padding(cellPadding));
        }

        for (int row = 0; row < board.Size; row++)
        {
            string[] rowCells = new string[board.Size + 1];
            rowCells[0] = $"[bold grey]{row + 1}[/]";

            for (int column = 0; column < board.Size; column++)
            {
                Position position = new(row, column);
                rowCells[column + 1] = FormatCellMarkup(board, position, rabbitState, visitedSet, status);
            }

            table.AddRow(rowCells);
        }

        return table;
    }

    private IRenderable BuildCompactTextGrid(
        ForestBoard board,
        RabbitState rabbitState,
        HashSet<Position> visitedSet,
        SimulationStatus status,
        bool constrainHeightToTerminal)
    {
        int rowNumberDigits = Math.Max(2, board.Size.ToString().Length);
        int rowPrefixWidth = rowNumberDigits + 2;

        int targetFullColumns = Math.Min(board.Size, MaxFullCompactGridSize);
        int requiredDenseWidth = rowPrefixWidth + (targetFullColumns * 2) + 4;

        if ((System.Console.IsOutputRedirected || !_console.Profile.Capabilities.Interactive) &&
            _console.Profile.Width > 0 &&
            _console.Profile.Width < requiredDenseWidth)
        {
            _console.Profile.Width = requiredDenseWidth;
        }

        int availableWidth = _console.Profile.Width > 0 ? _console.Profile.Width : 120;
        bool useSpacedColumns =
            board.Size <= MaxSpacedCompactGridSize &&
            (rowPrefixWidth + (board.Size * 3)) <= availableWidth;

        int visibleColumnCount;
        int visibleRowCount;

        if (useSpacedColumns)
        {
            visibleColumnCount = board.Size;
            visibleRowCount = board.Size;
        }
        else
        {
            int maxColsByWidth = Math.Max(1, (availableWidth - rowPrefixWidth) / 2);
            visibleColumnCount = Math.Min(board.Size, Math.Min(MaxFullCompactGridSize, maxColsByWidth));

            int maxRows = MaxFullCompactGridSize;
            if (constrainHeightToTerminal &&
                !System.Console.IsOutputRedirected &&
                _console.Profile.Capabilities.Interactive &&
                _console.Profile.Height > 0)
            {
                maxRows = Math.Min(maxRows, Math.Max(MinLiveViewportRows, _console.Profile.Height - LiveFrameOverheadRows));
            }

            visibleRowCount = Math.Min(board.Size, maxRows);
        }

        (int startRow, int endRow) = GetViewportBounds(board.Size, rabbitState.Position.Row, visibleRowCount);
        (int startColumn, int endColumn) = GetViewportBounds(board.Size, rabbitState.Position.Column, visibleColumnCount);

        List<IRenderable> lines = [];

        if (startRow > 0 || endRow < board.Size - 1 || startColumn > 0 || endColumn < board.Size - 1)
        {
            lines.Add(new Markup(
                $"[grey]Görünüm Kesiti: Satır {startRow + 1}..{endRow + 1} / {board.Size}, " +
                $"Sütun {startColumn + 1}..{endColumn + 1} / {board.Size}[/]"));
        }

        string headerIndent = new(' ', rowPrefixWidth);
        int columnCount = endColumn - startColumn + 1;

        if (useSpacedColumns)
        {
            string headerNumbers = string.Join(
                " ",
                Enumerable.Range(startColumn + 1, columnCount).Select(col => $"{col,2}"));
            lines.Add(new Markup($"[bold grey]{headerIndent}{headerNumbers}[/]"));
        }
        else
        {
            if (endColumn + 1 >= 100)
            {
                string hundredsHeader = string.Concat(
                    Enumerable.Range(startColumn + 1, columnCount)
                        .Select(col => col >= 100 ? $"{(col / 100) % 10} " : "  "));
                lines.Add(new Markup($"[bold grey]{headerIndent}{hundredsHeader}[/]"));
            }

            if (endColumn + 1 >= 10)
            {
                string tensHeader = string.Concat(
                    Enumerable.Range(startColumn + 1, columnCount)
                        .Select(col => col >= 10 ? $"{(col / 10) % 10} " : "  "));
                lines.Add(new Markup($"[bold grey]{headerIndent}{tensHeader}[/]"));
            }

            string unitsHeader = string.Concat(
                Enumerable.Range(startColumn + 1, columnCount)
                    .Select(col => $"{col % 10} "));
            lines.Add(new Markup($"[bold grey]{headerIndent}{unitsHeader}[/]"));
        }

        for (int row = startRow; row <= endRow; row++)
        {
            string rowLabel = (row + 1).ToString().PadLeft(rowNumberDigits);

            if (useSpacedColumns)
            {
                List<string> rowTokens = [$" [bold grey]{rowLabel}[/]"];

                for (int column = startColumn; column <= endColumn; column++)
                {
                    Position position = new(row, column);
                    rowTokens.Add(FormatCellMarkup(board, position, rabbitState, visitedSet, status, padToTwoChars: true));
                }

                lines.Add(new Markup(string.Join(" ", rowTokens)));
            }
            else
            {
                StringBuilder rowBuilder = new();
                rowBuilder.Append($" [bold grey]{rowLabel} [/]");

                for (int column = startColumn; column <= endColumn; column++)
                {
                    Position position = new(row, column);
                    rowBuilder.Append(FormatCellMarkup(board, position, rabbitState, visitedSet, status, padToTwoChars: true));
                }

                lines.Add(new Markup(rowBuilder.ToString()));
            }
        }

        return new Rows(lines);
    }

    private static (int Start, int End) GetViewportBounds(int totalSize, int focusIndex, int windowSize)
    {
        if (windowSize >= totalSize)
        {
            return (0, totalSize - 1);
        }

        int halfWindow = windowSize / 2;
        int start = Math.Clamp(focusIndex - halfWindow, 0, totalSize - windowSize);
        int end = start + windowSize - 1;
        return (start, end);
    }

    private static string FormatCellMarkup(
        ForestBoard board,
        Position position,
        RabbitState rabbitState,
        HashSet<Position> visitedSet,
        SimulationStatus status,
        bool padToTwoChars = false)
    {
        if (position == rabbitState.Position)
        {
            bool isAlive = rabbitState.IsAlive && status != SimulationStatus.Dead;
            string rabbitSymbol = GetRabbitSymbol(rabbitState.Direction, isAlive);

            if (!isAlive)
            {
                return $"[bold red]{rabbitSymbol}[/]";
            }

            if (status == SimulationStatus.Success)
            {
                return $"[bold springgreen1]{rabbitSymbol}[/]";
            }

            return $"[bold deepskyblue1]{rabbitSymbol}[/]";
        }

        string padSuffix = padToTwoChars ? " " : string.Empty;

        if (board.IsHole(position))
        {
            return $"[bold green]H{padSuffix}[/]";
        }

        ObstacleType obstacle = board.GetObstacleAt(position);
        switch (obstacle)
        {
            case ObstacleType.Wolf:
                return $"[bold red]W{padSuffix}[/]";

            case ObstacleType.Fox:
                return $"[bold indianred1]F{padSuffix}[/]";

            case ObstacleType.BarbedWire:
                return $"[bold yellow]T{padSuffix}[/]";

            case ObstacleType.Fence:
                return $"[bold darkorange]C{padSuffix}[/]";
        }

        if (visitedSet.Contains(position))
        {
            return $"[teal]•{padSuffix}[/]";
        }

        return $"[grey]·{padSuffix}[/]";
    }

    private static (string Headline, Color BorderColor, string? ExtraDetail) GetFinalStatusPresentation(
        ForestBoard board,
        SimulationResult result)
    {
        switch (result.Status)
        {
            case SimulationStatus.Success:
                return (
                    "TEBRİKLER! YILDIZ YUVASINA ULAŞTI.",
                    Color.Green,
                    null);

            case SimulationStatus.Dead:
                ObstacleType predator = board.GetObstacleAt(result.FinalPosition);
                string predatorDetail = predator switch
                {
                    ObstacleType.Wolf => "Karşılaşılan tehlike: Kurt (W).",
                    ObstacleType.Fox => "Karşılaşılan tehlike: Tilki (F).",
                    _ => "Karşılaşılan tehlike: Yırtıcı hayvan."
                };
                return (
                    "Yıldız bir kurt veya tilkiyle karşılaştı ve hayatını kaybetti.",
                    Color.Red,
                    predatorDetail);

            case SimulationStatus.Blocked:
                return (
                    "Yıldız engeli aşamadı. Senaryo başarısız oldu.",
                    Color.Yellow,
                    null);

            case SimulationStatus.OutOfBounds:
                return (
                    "Yıldız orman sınırlarının dışına çıkamaz. Senaryo sonlandırıldı.",
                    Color.Orange1,
                    null);

            case SimulationStatus.Incomplete:
            default:
                return (
                    "Senaryo tamamlandı ancak Yıldız henüz yuvasına ulaşamadı.",
                    Color.Aqua,
                    null);
        }
    }

    private static string FormatOneBasedPosition(Position position) =>
        $"({position.Row + 1}, {position.Column + 1})";

    private static string FormatDirectionText(Direction direction, bool isAlive = true)
    {
        string symbol = GetRabbitSymbol(direction, isAlive);
        string color = isAlive ? "deepskyblue1" : "red";
        string name = direction switch
        {
            Direction.South => "Güney",
            Direction.North => "Kuzey",
            Direction.East => "Doğu",
            Direction.West => "Batı",
            _ => "Güney"
        };

        return $"[bold {color}]{name} ({symbol})[/]";
    }

    private static string FormatDirectionPlainText(Direction direction, bool isAlive = true)
    {
        string symbol = GetRabbitSymbol(direction, isAlive);
        string name = direction switch
        {
            Direction.South => "Güney",
            Direction.North => "Kuzey",
            Direction.East => "Doğu",
            Direction.West => "Batı",
            _ => "Güney"
        };

        return $"{name} ({symbol})";
    }

    private static string FormatCommandText(ScenarioCommand command) => command switch
    {
        ScenarioCommand.MoveForward => "[bold cyan]N (İleri)[/]",
        ScenarioCommand.MoveBackward => "[bold cyan]P (Geri)[/]",
        ScenarioCommand.TurnRight => "[bold cyan]R (Sağa Dön)[/]",
        ScenarioCommand.TurnLeft => "[bold cyan]L (Sola Dön)[/]",
        ScenarioCommand.Jump => "[bold yellow]J (Zıplama Hazırlığı)[/]",
        ScenarioCommand.Duck => "[bold yellow]İ (Eğilme Hazırlığı)[/]",
        _ => command.ToString()
    };

    private static string FormatCommandShort(ScenarioCommand command) => command switch
    {
        ScenarioCommand.MoveForward => "N",
        ScenarioCommand.MoveBackward => "P",
        ScenarioCommand.TurnRight => "R",
        ScenarioCommand.TurnLeft => "L",
        ScenarioCommand.Jump => "J",
        ScenarioCommand.Duck => "İ",
        _ => command.ToString()
    };

    private static string FormatBypassText(ObstacleType? pendingBypass) => pendingBypass switch
    {
        ObstacleType.Fence => "[bold yellow]Zıplama hazırlığı (Çit için)[/]",
        ObstacleType.BarbedWire => "[bold yellow]Eğilme hazırlığı (Dikenli tel için)[/]",
        _ => "[grey]Yok[/]"
    };

    private static string FormatBypassPlainText(ObstacleType? pendingBypass) => pendingBypass switch
    {
        ObstacleType.Fence => "Zıplama hazırlığı (Çit)",
        ObstacleType.BarbedWire => "Eğilme hazırlığı (Dikenli tel)",
        _ => "Yok"
    };

    private static string FormatStatusText(SimulationStatus status) => status switch
    {
        SimulationStatus.Running => "[bold deepskyblue1]Devam ediyor[/]",
        SimulationStatus.Success => "[bold green]Başarılı[/]",
        SimulationStatus.Dead => "[bold red]Hayatını kaybetti[/]",
        SimulationStatus.Blocked => "[bold yellow]Engellendi[/]",
        SimulationStatus.OutOfBounds => "[bold orange1]Sınır İhlali[/]",
        SimulationStatus.Incomplete => "[bold aqua]Hedefe Ulaşılamadı[/]",
        _ => status.ToString()
    };

    private static string FormatStatusPlainText(SimulationStatus status) => status switch
    {
        SimulationStatus.Running => "Devam ediyor",
        SimulationStatus.Success => "Başarılı",
        SimulationStatus.Dead => "Hayatını kaybetti",
        SimulationStatus.Blocked => "Engellendi",
        SimulationStatus.OutOfBounds => "Sınır İhlali",
        SimulationStatus.Incomplete => "Hedefe Ulaşılamadı",
        _ => status.ToString()
    };
}
