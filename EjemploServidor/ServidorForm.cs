using System;
using System.Net;
using System.Windows.Forms;

namespace EjemploServidor
{
    public partial class ServidorForm : Form
    {
        Servidor servidor;

        public ServidorForm() => InitializeComponent();

        private void Log(string texto)
        {
            // Invoke nos permite ejecutar un delegado en el tread de la UI. 
            // El problema radica en que no es seguro interactuar con los controles
            // de Windows Forms desde múltiples threads. Y en este ejemplo, el 
            // método Log se está llamando desde eventos que se disparan desde
            // threads creados en el objeto Servidor.
            // Ver: https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/hSow-to-make-thread-safe-calls-to-windows-forms-controls
            Invoke((Action)delegate
            {
                txtLog.AppendText($"{DateTime.Now.ToLongTimeString()} - {texto.Trim()}");
                txtLog.AppendText(Environment.NewLine);
            });
        }

        private void ServidorForm_Load(object sender, EventArgs e)
        {
            if (!comboBoxDestinatarios.Items.Contains("Todos"))
            {
                comboBoxDestinatarios.Items.Insert(0, "Todos");
            }

            comboBoxDestinatarios.SelectedIndex = 0; // Selecciono el primer elemento del combo box

            // Inicializo el servidor estableciendo el puerto donde escuchar
            servidor = new Servidor(8050);

            // Me suscribo a los eventos
            servidor.NuevaConexion += Servidor_NuevaConexion;
            servidor.ConexionTerminada += Servidor_ConexionTerminada;
            servidor.DatosRecibidos += Servidor_DatosRecibidos;

            // Comienzo la escucha
            servidor.Escuchar();
        }

        private void Servidor_NuevaConexion(object sender, ServidorEventArgs e)
        {
            // Agrego el cliente al ComboBox de destinatarios (en el hilo de la UI)
            Invoke((Action)(() =>
            {
                var cliente = $"{e.EndPoint.Address}:{e.EndPoint.Port}";
                if (!comboBoxDestinatarios.Items.Contains(cliente))
                {
                    comboBoxDestinatarios.Items.Add(cliente);
                }
            }));

            // Muestro quién se conectó
            Log($"Se ha conectado un nuevo cliente desde la IP = {e.EndPoint.Address}, Puerto = {e.EndPoint.Port}");
        }

        private void Servidor_ConexionTerminada(object sender, ServidorEventArgs e)
        {
            // Remueve el cliente del ComboBox de destinatarios (en el hilo de la UI)
            Invoke((Action)(() =>
            {
                comboBoxDestinatarios.Items.Remove($"{e.EndPoint.Address}:{e.EndPoint.Port}");
            }));

            Log($"Se ha desconectado el cliente de la IP = {e.EndPoint.Address}, Puerto = {e.EndPoint.Port}");
        }

        private void Servidor_DatosRecibidos(object sender, DatosRecibidosEventArgs e)
        {
            // Muestro quién envió el mensaje
            Log($"Nuevo mensaje desde el cliente de la IP = {e.EndPoint.Address}, Puerto = {e.EndPoint.Port}");

            //  Muestro el mensaje recibido
            Log(e.DatosRecibidos);
        }

        private void BtnEnviarMensaje_Click(object sender, EventArgs e)
        {
            string destinatario = comboBoxDestinatarios.SelectedItem.ToString();
            string mensaje = txtMensaje.Text;

            if (destinatario == "Todos")
            {
                servidor.EnviarDatos(mensaje);
            }
            else
            {
                var partes = destinatario.Split(':');
                string ip = partes[0];
                int puerto = int.Parse(partes[1]);
                var endPoint = new IPEndPoint(IPAddress.Parse(ip), puerto);
                servidor.EnviarDatosACliente(endPoint, mensaje);
            }

            txtMensaje.Clear();
        }
    }
}
