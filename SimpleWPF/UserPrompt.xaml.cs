using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimpleWPF
{
    /// <summary>
    /// Interaction logic for UserPrompt.xaml
    /// </summary>
    public partial class UserPrompt : Window
    {
        public UserPrompt()
        {
            InitializeComponent();
        }

        TaskCompletionSource<String> tcs1 = new TaskCompletionSource<String>();
        public UserPrompt(string title, string input)
        {
            InitializeComponent();
            TitleText = title;
            InputText = input;
            Result = tcs1.Task;
        }

        protected override void OnClosed(EventArgs e)
        {
            tcs1.SetResult(InputTextBox.Text);
            base.OnClosed(e);
        }

        public string TitleText
        {
            get { return TitleTextBox.Text; }
            set { TitleTextBox.Text = value; }
        }

        public string InputText
        {
            get { return InputTextBox.Text; }
            set { InputTextBox.Text = value; }
        }

        public bool Canceled { get; set; }

        private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Canceled = true;
            Close();
        }

        private void BtnOk_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Canceled = false;
            Close();
        }

        public Task<String> Result;

    }


}
