using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TTSConsoleLib;

namespace SimpleWPF
{
    /// <summary>
    /// Interaction logic for ChatConsole.xaml
    /// </summary>
    public partial class ChatConsole : Window
    {

        public Main lib = new Main();
        public ChatConsole()
        {
            InitializeComponent();

            TTSConsoleLib.Main main = lib;
            main.GlobalSpeak = false;

            main.WriteLineToConsole +=(
                (x, v) =>
                {
                    Dispatcher.Invoke((Action)delegate ()
                    {
                        WriteTextToRichTextBox(x, v);
                        WriteText(Environment.NewLine, v);
                        richTextBox.ScrollToEnd();
                    });
                }
            );

            main.WriteToConsole += (
                (x, v) =>
                {
                    Dispatcher.Invoke((Action)delegate ()
                    {
                        WriteTextToRichTextBox(x, v);
                    });
                }
            );

            main.UserDialog += (
                async (x) =>
                {
                    UserPrompt dialog = null;
                    Dispatcher.Invoke((Action)delegate() 
                    {
                        dialog = new UserPrompt(x, String.Empty);
                        dialog.Show();
                    });
                    while (dialog == null)
                        Thread.Sleep(20);

                    return await dialog.Result;
                    //richTextBox.Document.Blocks.Add(new Paragraph(new Run(x) { }) { });
                }
            );

            main.Start();
        }

        private void WriteTextToRichTextBox(string x, ConsoleColor v)
        {
            if (TTSConsoleLib.Twitch.TwitchAPI.TextHasEmote(x))
            {
                var list = TTSConsoleLib.Twitch.TwitchAPI.ConvertText(x);
                foreach (var item in list)
                {
                    if(item.isText)
                        WriteText(item.Text, v);
                    else
                    {
                        WriteText(item.Image, v);
                        //richTextBox.Document.Blocks.Add(new BlockUIContainer(item.Image));
                        ///Paragraph para = new Paragraph();
                        //para.Inlines.Add("Some ");

                        //BitmapImage bitmap = new BitmapImage(new Uri(@"<your picture url>"));
                        //Image image = new Image();
                        //image.Source = bitmap;
                        //image.Width = 20;
                        //para.Inlines.Add(item.Image);

                        //para.Inlines.Add(" text");
                        //richTextBox.Document.Blocks.Add(para);
                    }
                }
            }
            else
            {
                WriteText(x, v);
            }
        }

        private void WriteText(string x, ConsoleColor v)
        {
            BrushConverter bc = new BrushConverter();
            TextRange tr = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd);
            tr.Text = x;
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                    bc.ConvertFromString(Enum.GetName(typeof(ConsoleColor), v)));
            }
            catch (FormatException) { }
        }
        private void WriteText(Image x, ConsoleColor v)
        {
            BrushConverter bc = new BrushConverter();
            TextRange tr = new TextRange(richTextBox.Document.ContentEnd, richTextBox.Document.ContentEnd);
            tr.Start.Paragraph.Inlines.Add(x);
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                    bc.ConvertFromString(Enum.GetName(typeof(ConsoleColor), v)));
            }
            catch (FormatException) { }
        }
    }
}
