using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Socket_demo
{
    /// <summary>
    /// Lógica de interacción para Visor.xaml
    /// </summary>
    public partial class Visor : Window
    {
        private string sucursal;
        private string turn;

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        public Visor()
        {
            InitializeComponent();
        }

        public void setDate(string sucursal, string turn) {
            this.sucursal = sucursal;
            this.turn = turn;

            this.lbSuc.Content = this.sucursal;
            this.lbTurn.Content = this.turn;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

            this.Top = 0;
            this.Left = SystemParameters.WorkArea.Right - this.Width;
        }
    }
}
