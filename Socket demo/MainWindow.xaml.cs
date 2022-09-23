using Microsoft.Reporting.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using WebSocketSharp;
using System.Drawing.Printing;
using System.Collections;
using Socket_demo.Models;

namespace Socket_demo
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Visor visor;
        WebSocket ws;
        NotifyIcon icono;
        Turn lastTurn;
        Thread backgroundThread;

        bool myStop = false;
        string messageTicket;
        public List<Brand> brands { get; set; }
        public List<Branch> branches { get; set; }

        //-------------------------------------------------------------------------------
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

            this.visor = new Visor();
            this.icono = new NotifyIcon();
            this.icono.Icon = Socket_demo.Properties.Resources.ticket;
            //this.icono.MouseClick += notifyIcon_Click;

            this.txtHost.Focus();
            this.visor.Show();
            this.visor.Hide();

            this.txtHost.Text = Socket_demo.Properties.Settings.Default.host;

            if (this.cb_brand.Items.Count > 0)
            {
                this.cb_brand.SelectedValue = Socket_demo.Properties.Settings.Default.Brand;
            }

            if (this.cbPrint.Items.Count > 0) {
                this.cbPrint.SelectedValue = Socket_demo.Properties.Settings.Default.print;
            }

            if (this.comboBox.Items.Count > 0) {
                this.comboBox.SelectedValue = Socket_demo.Properties.Settings.Default.Sucursal;
            }
            this.checkBox.IsChecked = Socket_demo.Properties.Settings.Default.ViewDesktop;

            if (Socket_demo.Properties.Settings.Default.host != string.Empty)
            {
                getBrands(Socket_demo.Properties.Settings.Default.host);
                getPrints();
            }
            else
            {
                this.txtHost.Focus();
            }

            if (Socket_demo.Properties.Settings.Default.host != string.Empty && Socket_demo.Properties.Settings.Default.Brand != string.Empty && Socket_demo.Properties.Settings.Default.Sucursal != string.Empty)
            {
                this.init();
            }
        }
        
        //private void notifyIcon_Click(object sender, System.Windows.Forms.MouseEventArgs e) {
        //    if(e.Button == MouseButtons.Left)
        //    {
                
        //    }
        //}

        private void Stop(object sender, EventArgs e)
        {
            this.myStop = true;
            if (this.ws != null)
            {
                this.ws.Close();
            }

            if (this.backgroundThread != null)
            {
                this.backgroundThread.Abort();
            }

            if (Socket_demo.Properties.Settings.Default.ViewDesktop)
            {
                this.visor.Hide();
            }

            Socket_demo.Properties.Settings.Default.Brand = string.Empty;
            Socket_demo.Properties.Settings.Default.Sucursal = string.Empty;
            Socket_demo.Properties.Settings.Default.print = string.Empty;
            Socket_demo.Properties.Settings.Default.host = string.Empty;
            Socket_demo.Properties.Settings.Default.ViewDesktop = false;
            Socket_demo.Properties.Settings.Default.Save();

            this.icono.Visible = false;
            this.Show();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowState = WindowState.Normal;
            this.Topmost = true;
            this.Focus();
            this.txtHost.Focus();
        }

        private void LastTurnPrint(object sender, EventArgs e)
        {
            if (this.lastTurn != null)
            {
                this.printingShift(this.lastTurn);
            }
            else
            {
                this.icono.ShowBalloonTip(5000, "TicketsPrint", $"No existe un último turno impreso en memoria.", ToolTipIcon.Warning);
            }
        }
        
        private void ReconnectSocket() {
            while (true) {
                if (this.ws != null && this.ws.ReadyState == WebSocketState.Closed && !this.myStop)
                {
                    this.ws.Connect();
                }
            }
        }
        // Faltaria mostrar los nombre en lugar de los idBrand y idBranch
        private void TestPrint(object sender, EventArgs e) {
            try {
                string status = this.ws.ReadyState == WebSocketState.Open ? "Conectado" : "Desconectado";

                ReportParameter[] paramenters = new ReportParameter[]
                {
                    new ReportParameter("print", Socket_demo.Properties.Settings.Default.print),
                    new ReportParameter("status", status),
                    new ReportParameter("branch", Socket_demo.Properties.Settings.Default.Sucursal)
                };

                this.printReport(paramenters, "Report2.rdlc");
            }
            catch (Exception ex) {
                this.icono.ShowBalloonTip(5000, "TicketsPrint", ex.Message, ToolTipIcon.Error);
            }
        }

        private void TestConnect(object sender, EventArgs e) {
            bool state = this.ws.Ping();
            ToolTipIcon icon = state ? ToolTipIcon.None : ToolTipIcon.Error;
            this.icono.ShowBalloonTip(5000, "TicketsPrint", $"Estado de la conexion: {state}", icon);
        }
        
        public void getPrints() {
            ArrayList prints = new ArrayList();

            for (int i = 0; i < PrinterSettings.InstalledPrinters.Count; i++)
            {
                PrinterSettings a = new PrinterSettings();
                prints.Add(PrinterSettings.InstalledPrinters[i].ToString());
            }

            this.cbPrint.ItemsSource = prints;
        }

        public void getData<T>(string url, System.Windows.Controls.ComboBox cb)
        {
            try
            {
                string json = string.Empty;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }

                ResponseWreapper<List<T>> jsonResponse = JsonConvert.DeserializeObject<ResponseWreapper<List<T>>>(json);

                Array.Sort(jsonResponse.body.ToArray(), new ResponseWreapperComparer<T>());

                if (typeof(T) == typeof(Brand))
                {
                    this.brands = new List<Brand>();
                }
                else if (typeof(T) == typeof(Branch))
                {
                    this.branches = new List<Branch>();
                }
                
                foreach (var item in jsonResponse.body)
                {
                    if (typeof(T) == typeof(Brand))
                    {
                        Brand data = item as Brand;
                        this.brands.Add(data);
                    }
                    else if (typeof(T) == typeof(Branch))
                    {
                        Branch data = item as Branch;
                        this.branches.Add(data);
                    }
                }

                if (typeof(T) == typeof(Brand))
                {
                    cb.ItemsSource = this.brands;
                }
                else if (typeof(T) == typeof(Branch))
                {
                    cb.ItemsSource = this.branches;
                }
                
                cb.DisplayMemberPath = "name";
                cb.SelectedValuePath = "_id";
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void getBrands(string host)
        {
            string url = $@"http://{host}:4000/api/v1/brands";
            getData<Brand>(url, this.cb_brand);
        }

        public void getSucursals(string host, string idBrand) {
            string url = $@"http://{host}:4000/api/v1/brands/{idBrand}/branches";
            getData<Branch>(url, this.comboBox);
        }

        public void init(bool isNew = false) {
            try
            {
                string json = string.Empty;
                string url = !isNew ? $@"http://{Socket_demo.Properties.Settings.Default.host}:4000/api/v1/brands/{Socket_demo.Properties.Settings.Default.Brand}/branches/{Socket_demo.Properties.Settings.Default.Sucursal}" :
                    $@"http://{this.txtHost.Text}:4000/api/v1/brands/{this.cb_brand.SelectedValue}/branches/{this.comboBox.SelectedValue}";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }

                ResponseWreapper<Branch> jsonResponse = JsonConvert.DeserializeObject<ResponseWreapper<Branch>>(json);
                
                this.messageTicket = jsonResponse.body.messageTicket;

                if (isNew) {
                    Socket_demo.Properties.Settings.Default.Brand = this.cb_brand.SelectedValue.ToString();
                    Socket_demo.Properties.Settings.Default.Sucursal = this.comboBox.SelectedValue.ToString();
                    Branch branch = (Branch)this.comboBox.SelectedItem;
                    Socket_demo.Properties.Settings.Default.BranchName = branch.name;
                    Socket_demo.Properties.Settings.Default.print = this.cbPrint.SelectedValue.ToString();
                    Socket_demo.Properties.Settings.Default.host = this.txtHost.Text;
                    Socket_demo.Properties.Settings.Default.ViewDesktop = (bool)this.checkBox.IsChecked;
                    Socket_demo.Properties.Settings.Default.Save();
                }


                //this.ws = new WebSocket(url: "ws://192.168.1.14:7000");
                string urlSocket = !isNew ? $"ws://{Socket_demo.Properties.Settings.Default.host}:4000" : $"ws://{this.txtHost.Text}:4000";
                this.ws = new WebSocket(url: urlSocket);
                this.ws.OnOpen += Ws_OnOpen;
                this.ws.OnMessage += Ws_OnMessage;
                this.ws.OnError += Ws_OnError;
                this.ws.Connect();

                this.WindowState = WindowState.Minimized;

                if (this.ShowActivated) {
                    this.Hide();
                    if (Socket_demo.Properties.Settings.Default.ViewDesktop)
                    {
                        this.visor.setDate(Socket_demo.Properties.Settings.Default.BranchName, "");
                        this.visor.Show();
                    }
                    
                    ContextMenu contextMenu = new ContextMenu();
                    contextMenu.MenuItems.Add("Detener", new EventHandler(Stop));
                    contextMenu.MenuItems.Add("Reimprimir último turno", new EventHandler(LastTurnPrint));
                    contextMenu.MenuItems.Add("Test conexion", new EventHandler(TestConnect));
                    contextMenu.MenuItems.Add("Test impresion", new EventHandler(TestPrint));
                    this.icono.ContextMenu = contextMenu;
                    this.icono.Visible = true;
                    this.icono.ShowBalloonTip(5000, "TicketsPrint", $"La aplicación de impresión de tickets para el toma turnos esta en al barra de notificaciones, Print:{Socket_demo.Properties.Settings.Default.print}", ToolTipIcon.None);
                }

                this.backgroundThread = new Thread(new ThreadStart(this.ReconnectSocket));
                this.backgroundThread.Start();
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //-------------------------------------------------------------------------------
        private void Ws_OnOpen(object sender, EventArgs e)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("idBrand", Socket_demo.Properties.Settings.Default.Brand);
            data.Add("idBranch", Socket_demo.Properties.Settings.Default.Sucursal);
            var body = new {
                acction = "suscribe",
                data = data
            };
            var json = JsonConvert.SerializeObject(body);
            this.ws.Send(json.ToString());
        }

        private void Ws_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            System.Windows.MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                ResponseWrapperSocket turnResponse = JsonConvert.DeserializeObject<ResponseWrapperSocket>(e.Data);
                this.lastTurn = JsonConvert.DeserializeObject<Turn>(turnResponse.info);
                this.printingShift(this.lastTurn);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void printingShift(Turn turn)
        {
            if (Socket_demo.Properties.Settings.Default.ViewDesktop)
            {
                Dispatcher.Invoke(() =>
                {
                    if (this.visor.Visibility == Visibility.Hidden)
                    {
                        visor.Show();
                    }

                    this.visor.setDate(turn.branch.name, turn.turn);
                });
            }
            else
            {
                DateTime dateValue;
                if (!DateTime.TryParse(turn.createdAt, out dateValue))
                {
                    dateValue = DateTime.Now;
                }

                ReportParameter[] paramenters = new ReportParameter[]
                {
                    new ReportParameter("sucursal", turn.branch.name),
                    new ReportParameter("turno", turn.turn),
                    new ReportParameter("fecha", dateValue.ToString()),
                    new ReportParameter("message", this.messageTicket)
                };

                this.printReport(paramenters, "Report1.rdlc");
            }
        }
        //-------------------------------------------------------------------------------
        private void printReport(ReportParameter[] parameters, string rdl) {
            LocalReport rdlc = new LocalReport();
            rdlc.ReportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rdl);
            rdlc.SetParameters(parameters);

            Impresion imp = new Impresion(Socket_demo.Properties.Settings.Default.print);
            imp.Imprime(rdlc);
        }
        
        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (this.cb_brand.SelectedIndex > -1 && this.comboBox.SelectedIndex > -1 && this.cbPrint.SelectedIndex > -1 && this.txtHost.Text != string.Empty)
            {
                this.myStop = false;
                init(true);
            }
            else {
                System.Windows.MessageBox.Show("Todos los campos son obligatorios.", "Precaución", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.backgroundThread != null) {
                this.backgroundThread.Abort();
            }
            
            this.myStop = true;
            if (this.ws != null) {
                this.ws.Close();
            }

            this.visor.Close();
        }
        
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (this.txtHost.Text != string.Empty)
            {
                //getSucursals(this.txtHost.Text);
                getBrands(this.txtHost.Text);
                getPrints();
            }
            else {
                System.Windows.MessageBox.Show("El campo Host es obligatorio.", "Precaución", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void txtHost_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            this.cbPrint.ItemsSource = null;
            this.comboBox.ItemsSource = null;
            this.cb_brand.ItemsSource = null;
        }

        private void cb_brand_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.comboBox.ItemsSource = null;
            System.Windows.Controls.ComboBox cb = sender as System.Windows.Controls.ComboBox;
            getSucursals(this.txtHost.Text, cb.SelectedValue.ToString());
        }
        //-------------------------------------------------------------------------------
    }
}
