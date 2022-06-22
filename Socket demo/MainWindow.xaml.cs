using Microsoft.Reporting.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using WebSocketSharp;

namespace Socket_demo
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WebSocket ws;
        string print;
        string messageTicket;
        NotifyIcon icono;
        Visor visor = new Visor();
        WrapperTurn lastTurn = null;

        public List<Sucursal> sucursales { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            //cambiar de:
            //xmlns="http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"
            //a:
            //xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition"
            //Remover etiqueta <ReportParametersLayout> junto con su contenido
            //Remover etiquetas <ReportSections> y <ReportSection> sin con su contenido (respetar etiqueta <body>)


            this.icono = new NotifyIcon();
            this.icono.Icon = Socket_demo.Properties.Resources.ticket;
            this.icono.MouseClick += notifyIcon_Click;
            //this.textBox.Focus();
            this.comboBox.Focus();

            this.visor.Show();
            this.visor.Hide();

            getSucursals();
            if (Socket_demo.Properties.Settings.Default.Sucursal != string.Empty)
            {
                init();
            }
            else 
            { 
                this.checkBox.IsChecked = Socket_demo.Properties.Settings.Default.ViewDesktop;
            }
        }

        private void notifyIcon_Click(object sender, System.Windows.Forms.MouseEventArgs e) {
            if(e.Button == MouseButtons.Left)
            {
                
            }
        }

        private void Close(object sender, EventArgs e)
        {
            if (this.ws != null)
            {
                this.ws.Close();
            }

            if (Socket_demo.Properties.Settings.Default.ViewDesktop)
            {
                this.visor.Hide();
            }
            
            
            Socket_demo.Properties.Settings.Default.Sucursal = string.Empty;
            Socket_demo.Properties.Settings.Default.Save();
            this.icono.Visible = false;
            this.Show();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowState = WindowState.Normal;
            this.Topmost = true;
            this.Focus();
            //this.textBox.Focus();
            this.comboBox.Focus();
        }

        private void LastTurnPrint(object sender, EventArgs e)
        {
            if (this.lastTurn != null)
            {
                this.printing(this.lastTurn);
            }
            else
            {
                this.icono.ShowBalloonTip(5000, "TicketsPrint", $"No existe un último turno impreso en memoria.", ToolTipIcon.Warning);
            }
        }

        public void getSucursals() {
            try
            {
                string host = "172.25.200.8";
                string url = $@"http://{host}:4000/api/sucursal";
                string json = string.Empty;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers["me"] = "";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }

                ConfigSucursales configSucursales = JsonConvert.DeserializeObject<ConfigSucursales>(json);

                Array.Sort(configSucursales.body, new SucursalComparer());

                this.sucursales = new List<Sucursal>(configSucursales.body);
                //comboBox.ItemsSource = configSucursales.body;
                comboBox.ItemsSource = this.sucursales;
                comboBox.DisplayMemberPath = "name";
                comboBox.SelectedValuePath = "name";
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void init() {
            try
            {
                //192.168.1.14:4000
                //Moreira
                //10.0.0.50
                string host = "172.25.200.8";
                //string host = "localhost";
                //Angelópolis
                //string sucursal = Socket_demo.Properties.Settings.Default.Sucursal != string.Empty ? Socket_demo.Properties.Settings.Default.Sucursal : textBox.Text;
                string sucursal = Socket_demo.Properties.Settings.Default.Sucursal != string.Empty ? Socket_demo.Properties.Settings.Default.Sucursal : this.comboBox.SelectedValue.ToString();
                string html = string.Empty;
                string url = $@"http://{host}:4000/api/sucursal/{sucursal}";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers["me"] = "";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }

                ConfigSucursal configSucursal = JsonConvert.DeserializeObject<ConfigSucursal>(html);
                this.print = configSucursal.body.print;
                this.messageTicket = configSucursal.body.messageTicket;

                if (Socket_demo.Properties.Settings.Default.Sucursal == string.Empty) {
                    Socket_demo.Properties.Settings.Default.Sucursal = sucursal;
                    Socket_demo.Properties.Settings.Default.Save();
                }

                //this.ws = new WebSocket(url: "ws://192.168.1.14:7000");
                this.ws = new WebSocket(url: $"ws://{host}:7000");
                this.ws.OnOpen += Ws_OnOpen;
                this.ws.OnMessage += Ws_OnMessage;
                this.ws.OnError += Ws_OnError;
                this.ws.Connect();

                this.WindowState = WindowState.Minimized;

                if (this.ShowActivated) {
                    this.Hide();
                    if (Socket_demo.Properties.Settings.Default.ViewDesktop)
                    {
                        this.visor.setDate(sucursal, "");
                        this.visor.Show();
                    }
                    
                    ContextMenu contextMenu = new ContextMenu();
                    contextMenu.MenuItems.Add("Detener", new EventHandler(Close));
                    contextMenu.MenuItems.Add("Reimprimir último turno", new EventHandler(LastTurnPrint));
                    this.icono.ContextMenu = contextMenu;
                    this.icono.Visible = true;
                    this.icono.ShowBalloonTip(5000, "TicketsPrint", $"La aplicación de impresión de tickets para el tomaturnos esta en al barra de notificaciones, Print:{this.print}", ToolTipIcon.None);
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Ws_OnOpen(object sender, EventArgs e)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("acction", "suscribe");
            data.Add("sucursal", this.comboBox.SelectedValue.ToString());
            var json = JsonConvert.SerializeObject(data);
            this.ws.Send(json.ToString());
        }

        private void Ws_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Console.Write("Error: {0}, Exception: {1}", e.Message, e.Exception);
        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            WrapperTurn turn = JsonConvert.DeserializeObject<WrapperTurn>(e.Data);
            this.lastTurn = turn;
            this.printing(turn);
        }

        private void printing(WrapperTurn turn)
        {
            if (Socket_demo.Properties.Settings.Default.ViewDesktop)
            {
                Dispatcher.Invoke(() =>
                {
                    if (this.visor.Visibility == Visibility.Hidden)
                    {
                        visor.Show();
                    }

                    this.visor.setDate(turn.turn.sucursal, turn.turn.turn);
                });
            }
            else
            {
                //Dispatcher.Invoke(() =>
                //{
                //    this.icono.ShowBalloonTip(5000, "TicketsPrint", $"Ticket '{turn.turn.turn.ToUpper()}' impreso en: {this.print}, Ruta: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Report1.rdlc")}", ToolTipIcon.None);
                //});

                DateTime dateValue;
                if (!DateTime.TryParse(turn.turn.creationDate, out dateValue))
                {
                    dateValue = DateTime.Now;
                }

                LocalReport rdlc = new LocalReport();
                ReportParameter[] p = new ReportParameter[]
                {
                new ReportParameter("sucursal", turn.turn.sucursal),
                new ReportParameter("turno", turn.turn.turn),
                new ReportParameter("fecha", dateValue.ToString()),
                new ReportParameter("message", this.messageTicket)
                };

                rdlc.ReportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Report1.rdlc");
                rdlc.SetParameters(p);

                Impresion imp = new Impresion(this.print);
                imp.Imprime(rdlc);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            
            //if (textBox.Text != string.Empty)
            if (this.comboBox.SelectedIndex > -1)
            {
                init();
            }
            else {
                System.Windows.MessageBox.Show("El campo 'Sucursal' es obligatorio.", "Precaución", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.ws != null) {
                this.ws.Close();
            }

            this.visor.Close();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            //switch (this.WindowState)
            //{
            //    case WindowState.Minimized:
            //        this.Hide();
            //        ContextMenu contextMenu = new ContextMenu();
            //        contextMenu.MenuItems.Add("Detener", new EventHandler(Close));
            //        this.icono.ContextMenu = contextMenu;
            //        this.icono.Visible = true;
            //        this.icono.ShowBalloonTip(5000, "TicketsPrint", "La aplicacion de impresion de tickets para el tomaturnos esta en al barra de notificaciones", ToolTipIcon.None);
            //        break;
            //}
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            Socket_demo.Properties.Settings.Default.ViewDesktop = true;
            Socket_demo.Properties.Settings.Default.Save();
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Socket_demo.Properties.Settings.Default.ViewDesktop = false;
            Socket_demo.Properties.Settings.Default.Save();
        }
    }
}
