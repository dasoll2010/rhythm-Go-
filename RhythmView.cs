using System.Drawing.Drawing2D;

namespace RhythmGo;

public sealed class RhythmForm : Form
{
    private enum ScreenMode { Main, Game, Editor, KeyViewer }
    private ScreenMode mode = ScreenMode.Main;
    private readonly List<TileData> tiles = new();
    private readonly HashSet<Keys> pressed = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private int progress;
    private bool debugMode;
    private bool autoPlay;
    private bool editorWarningShown;
    private string typed = "";
    private Point mouse;
    private int selectedTile = -1;
    private bool dragging;
    private Point dragOffset;

    public RhythmForm()
    {
        Text = "rhythm Go!";
        ClientSize = new Size(960, 600);
        MinimumSize = new Size(720, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(18, 19, 25);
        DoubleBuffered = true;
        KeyPreview = true;

        for (int i = 0; i < 10; i++)
            tiles.Add(new TileData { X = 170 + i * 66, Y = 300 });

        timer.Tick += (_, _) =>
        {
            if (mode == ScreenMode.Game && autoPlay && progress < 10)
                progress++;
            Invalidate();
        };
        timer.Start();

        KeyDown += HandleKeyDown;
        KeyUp += (_, e) => pressed.Remove(e.KeyCode);
        MouseMove += HandleMouseMove;
        MouseDown += HandleMouseDown;
        MouseUp += (_, _) => dragging = false;
        MouseClick += HandleMouseClick;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        pressed.Add(e.KeyCode);
        if (mode == ScreenMode.KeyViewer) { Invalidate(); return; }

        if (e.KeyCode == Keys.Escape)
        {
            mode = ScreenMode.Main;
            Invalidate();
            return;
        }

        if (mode == ScreenMode.Main || mode == ScreenMode.Game)
        {
            if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z || e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
            {
                typed += e.KeyCode.ToString();
                if (typed.Length > 10) typed = typed[^10..];
                if (typed.EndsWith("THISDEBUG1", StringComparison.OrdinalIgnoreCase))
                    debugMode = true;
            }
        }
        Invalidate();
    }

    private void HandleMouseMove(object? sender, MouseEventArgs e)
    {
        mouse = e.Location;
        if (mode == ScreenMode.Editor && dragging && selectedTile >= 0)
        {
            tiles[selectedTile].X = e.X - dragOffset.X;
            tiles[selectedTile].Y = e.Y - dragOffset.Y;
        }
        Invalidate();
    }

    private void HandleMouseDown(object? sender, MouseEventArgs e)
    {
        if (mode != ScreenMode.Editor || e.Button != MouseButtons.Left) return;
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            var r = TileRect(tiles[i]);
            if (r.Contains(e.Location))
            {
                selectedTile = i;
                dragOffset = new Point(e.X - (int)tiles[i].X, e.Y - (int)tiles[i].Y);
                dragging = true;
                return;
            }
        }
    }

    private void HandleMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (mode == ScreenMode.Main)
        {
            if (ButtonRect("MAIN", 40, 40).Contains(e.Location)) return;
            if (ButtonRect("STAGE 1", 370, 260).Contains(e.Location))
            {
                progress = 0; mode = ScreenMode.Game;
            }
            else if (debugMode && ButtonRect("Level Editor", 370, 330).Contains(e.Location))
            {
                mode = ScreenMode.Editor;
                if (!editorWarningShown)
                {
                    editorWarningShown = true;
                    MessageBox.Show(this, "경고: 레벨에디터는 최적화되지 않음", "rhythm Go!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (debugMode && ButtonRect("Key Viewer", 370, 390).Contains(e.Location))
                mode = ScreenMode.KeyViewer;
        }
        else if (mode == ScreenMode.Game)
        {
            if (progress < 10) progress++;
            if (progress >= 10)
            {
                MessageBox.Show(this, "SUCCESS!", "rhythm Go!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                mode = ScreenMode.Main;
            }
        }
        else if (mode == ScreenMode.Editor)
        {
            if (ButtonRect("ADD", 20, 80).Contains(e.Location))
                tiles.Add(new TileData { X = 200 + tiles.Count * 25, Y = 300 });
            else if (ButtonRect("ROTATE 90", 20, 130).Contains(e.Location) && selectedTile >= 0)
                tiles[selectedTile].Rotation = (tiles[selectedTile].Rotation + 90) % 360;
            else if (ButtonRect("CHECKMATE", 20, 180).Contains(e.Location) && selectedTile >= 0)
                tiles[selectedTile].Checkmate = !tiles[selectedTile].Checkmate;
            else if (ButtonRect("EFFECT", 20, 230).Contains(e.Location) && selectedTile >= 0)
                tiles[selectedTile].Effect = !tiles[selectedTile].Effect;
            else if (ButtonRect("AUTO PLAY", 20, 280).Contains(e.Location))
                autoPlay = !autoPlay;
            else if (ButtonRect("PLAY", 20, 330).Contains(e.Location))
            {
                progress = 0; mode = ScreenMode.Game;
            }
            else if (ButtonRect("SAVE", 20, 380).Contains(e.Location))
            {
                SaveEditor();
                MessageBox.Show(this, "앱 데이터에 저장했어!", "rhythm Go!");
            }
        }
    }

    private void SaveEditor()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "rhythm-Go", "level.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(tiles));
    }

    private RectangleF TileRect(TileData t) => new(t.X - 22, t.Y - 22, 44, 44);

    private Rectangle ButtonRect(string text, int x, int y)
    {
        using var f = new Font("Segoe UI", 11, FontStyle.Bold);
        var size = TextRenderer.MeasureText(text, f);
        return new Rectangle(x, y, Math.Max(140, size.Width + 34), 42);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(18, 19, 25));
        DrawHeader(g);

        switch (mode)
        {
            case ScreenMode.Main: DrawMain(g); break;
            case ScreenMode.Game: DrawGame(g); break;
            case ScreenMode.Editor: DrawEditor(g); break;
            case ScreenMode.KeyViewer: DrawKeyViewer(g); break;
        }
    }

    private void DrawHeader(Graphics g)
    {
        using var titleFont = new Font("Segoe UI", 22, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        g.DrawString("rhythm Go!", titleFont, brush, 28, 18);
        if (debugMode)
        {
            using var debugFont = new Font("Segoe UI", 10, FontStyle.Bold);
            using var green = new SolidBrush(Color.LimeGreen);
            g.DrawString("Debug Mode", debugFont, green, 28, 55);
        }
    }

    private void DrawMain(Graphics g)
    {
        DrawButton(g, "STAGE 1", 370, 260);
        using var f = new Font("Segoe UI", 12);
        using var b = new SolidBrush(Color.Gainsboro);
        g.DrawString("키보드로 thisdebug1 입력하면 디버그 메뉴가 열려.", f, b, 300, 215);
        if (debugMode)
        {
            DrawButton(g, "Level Editor", 370, 330);
            DrawButton(g, "Key Viewer", 370, 390);
        }
    }

    private void DrawGame(Graphics g)
    {
        using var f = new Font("Segoe UI", 13, FontStyle.Bold);
        using var b = new SolidBrush(Color.White);
        g.DrawString($"STAGE 1   {progress}/10", f, b, 40, 100);
        for (int i = 0; i < 10; i++)
        {
            var r = new RectangleF(140 + i * 68, 290, 48, 48);
            using var tileBrush = new SolidBrush(i < progress ? Color.MediumSeaGreen : Color.FromArgb(75, 82, 100));
            g.FillRectangle(tileBrush, r);
            g.DrawRectangle(Pens.White, r.X, r.Y, r.Width, r.Height);
            if (i == Math.Min(progress, 9)) DrawRainbowBall(g, r.X + r.Width / 2, r.Y - 18, 11);
        }
        using var hint = new SolidBrush(Color.LightGray);
        g.DrawString("화면을 클릭해서 타일을 진행해!", new Font("Segoe UI", 12), hint, 360, 390);
    }

    private void DrawEditor(Graphics g)
    {
        DrawButton(g, "ADD", 20, 80);
        DrawButton(g, "ROTATE 90", 20, 130);
        DrawButton(g, "CHECKMATE", 20, 180);
        DrawButton(g, "EFFECT", 20, 230);
        DrawButton(g, "AUTO PLAY", 20, 280);
        DrawButton(g, "PLAY", 20, 330);
        DrawButton(g, "SAVE", 20, 380);
        for (int i = 0; i < tiles.Count; i++)
        {
            var t = tiles[i];
            var state = g.Save();
            g.TranslateTransform(t.X, t.Y);
            g.RotateTransform(t.Rotation);
            var rect = new RectangleF(-22, -22, 44, 44);
            using var fill = new SolidBrush(i == selectedTile ? Color.Gold : t.Checkmate ? Color.MediumSeaGreen : Color.SlateBlue);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(Pens.White, rect.X, rect.Y, rect.Width, rect.Height);
            if (t.Effect) g.DrawEllipse(Pens.Orange, -28, -28, 56, 56);
            g.Restore(state);
        }
        using var info = new SolidBrush(Color.White);
        g.DrawString("블럭을 클릭하고 드래그해서 이동할 수 있어.", new Font("Segoe UI", 11), info, 250, 500);
    }

    private void DrawKeyViewer(Graphics g)
    {
        using var f = new Font("Segoe UI", 12, FontStyle.Bold);
        using var b = new SolidBrush(Color.White);
        g.DrawString("Key Viewer — 키보드를 눌러 봐. ESC: 메인으로", f, b, 40, 100);
        int x = 40, y = 150;
        foreach (var key in pressed.OrderBy(k => k.ToString()).Take(35))
        {
            DrawButton(g, key.ToString(), x, y, 100);
            x += 115;
            if (x > ClientSize.Width - 110) { x = 40; y += 55; }
        }
    }

    private void DrawButton(Graphics g, string text, int x, int y, int width = 180)
    {
        var r = new Rectangle(x, y, width, 42);
        using var fill = new SolidBrush(Color.FromArgb(42, 47, 62));
        using var pen = new Pen(Color.FromArgb(120, 145, 190), 1.5f);
        using var font = new Font("Segoe UI", 11, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        g.FillRoundedRectangle(fill, r, 9);
        g.DrawRoundedRectangle(pen, r, 9);
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, textBrush, x + (width - size.Width) / 2, y + (42 - size.Height) / 2);
    }

    private void DrawRainbowBall(Graphics g, float x, float y, float radius)
    {
        Color[] colors = [Color.Red, Color.Orange, Color.Yellow, Color.Lime, Color.DeepSkyBlue, Color.Blue, Color.Magenta];
        for (int i = 0; i < colors.Length; i++)
        {
            using var brush = new SolidBrush(colors[i]);
            float angle = i * 360f / colors.Length;
            float a = angle * MathF.PI / 180f;
            g.FillEllipse(brush, x + MathF.Cos(a) * 5 - radius / 2, y + MathF.Sin(a) * 5 - radius / 2, radius, radius);
        }
        g.DrawEllipse(Pens.White, x - radius / 2 - 3, y - radius / 2 - 3, radius + 6, radius + 6);
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = Rounded(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = Rounded(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
