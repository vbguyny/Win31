using ProgramManager31.Controls;
using ProgramManager31.Icons;

namespace ProgramManager31.Dialogs;

internal sealed class AboutDialog : ClassicDialog
{
    public AboutDialog()
    {
        Text = "About Program Manager";
        SetContentSize(ClassicTheme.S(420), ClassicTheme.S(176));

        var icon = new PictureBox
        {
            Bounds = new Rectangle(ClassicTheme.S(16), ClassicTheme.S(18), ClassicTheme.S(48), ClassicTheme.S(48)),
            BackColor = ClassicTheme.Face
        };
        icon.Paint += (_, e) =>
            IconPainter.DrawIcon(e.Graphics, "group",
                new Rectangle(0, 0, icon.Width, icon.Height));

        var title = new ClassicLabel
        {
            Text = "Program Manager",
            Font = ClassicTheme.UIBold,
            Bounds = new Rectangle(ClassicTheme.S(78), ClassicTheme.S(18), ClassicTheme.S(300), ClassicTheme.S(18)),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var subtitle = new ClassicLabel
        {
            Text = "Windows 3.1 style recreation",
            Bounds = new Rectangle(ClassicTheme.S(78), ClassicTheme.S(38), ClassicTheme.S(300), ClassicTheme.S(16)),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var version = new ClassicLabel
        {
            Text = $"Version 3.1  \u00b7  {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}",
            Bounds = new Rectangle(ClassicTheme.S(78), ClassicTheme.S(56), ClassicTheme.S(320), ClassicTheme.S(16)),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var info = new ClassicLabel
        {
            Text = "An independent re-creation in C# and .NET 10.\r\n" +
                   "Not affiliated with Microsoft Corporation.\r\n" +
                   "All icons and artwork are original drawings.",
            WordWrap = true,
            Bounds = new Rectangle(ClassicTheme.S(18), ClassicTheme.S(80), ClassicTheme.S(384), ClassicTheme.S(54))
        };

        var ok = new ClassicButton
        {
            Text = "OK",
            Bounds = new Rectangle(ClassicTheme.S(336), ClassicTheme.S(140), ClassicTheme.S(68), ClassicTheme.ButtonHeight),
            TabIndex = 0
        };
        ok.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        Body.Controls.Add(icon);
        Body.Controls.Add(title);
        Body.Controls.Add(subtitle);
        Body.Controls.Add(version);
        Body.Controls.Add(info);
        Body.Controls.Add(ok);
        DefaultButton = ok;
    }
}
