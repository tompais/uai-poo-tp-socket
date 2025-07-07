using System;
using System.Linq;
using System.Windows.Forms;

namespace EjemploCliente
{
    public partial class ClienteForm : Form
    {
        private readonly Cliente cliente = new Cliente();

        public ClienteForm() => InitializeComponent();

        private void Log(string texto)
        {
            // Invoke nos permite ejecutar un delegado en el tread de la UI. 
            // El problema radica en que no es seguro interactuar con los controles
            // de Windows Forms desde múltiples threads. Y en este ejemplo, el 
            // método Log se está llamando desde eventos que se disparan desde
            // threads creados en el objeto Cliente.
            // Ver: https://docs.microsoft.com/en-us/dotnet/framework/winforms/controls/how-to-make-thread-safe-calls-to-windows-forms-controls
            Invoke((Action)delegate
            {
                txtLog.AppendText($"{DateTime.Now.ToLongTimeString()} - {texto.Trim()}");
                txtLog.AppendText(Environment.NewLine);
            });
        }

        private void ClienteForm_Load(object sender, EventArgs e)
        {
            comboBoxDestinatarios.SelectedIndex = 0; // Selecciono el primer elemento del combo box

            // Creo una instancia de Cliente y me suscribo a los eventos
            cliente.ConexionTerminada += Cliente_ConexionTerminada;
            cliente.DatosRecibidos += Cliente_DatosRecibidos;
        }

        private void Cliente_DatosRecibidos(object sender, DatosRecibidosEventArgs e)
        {
            if (e.DatosRecibidos.StartsWith("CLIENTES:"))
            {
                var lista = e.DatosRecibidos.Substring(9).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                Invoke((Action)(() =>
                {
                    comboBoxDestinatarios.Items.Clear();
                    comboBoxDestinatarios.Items.Add("Todos");

                    var local = cliente.LocalEndPoint;
                    var ip = local.Address;
                    if (ip.IsIPv4MappedToIPv6)
                    {
                        ip = ip.MapToIPv4();
                    }

                    var miDireccion = $"{ip}:{local.Port}";
                    lista.Where(cli => !string.Equals(cli, miDireccion)).ToList().ForEach(cli => comboBoxDestinatarios.Items.Add(cli));

                    comboBoxDestinatarios.SelectedIndex = 0;
                }));
            }
            else
            {
                Log($"El servidor envió el siguiente mensaje: {e.DatosRecibidos}");
            }
        }

        private void Cliente_ConexionTerminada(object sender, EventArgs e)
        {
            Log("Finalizó la conexión");

            UpdateUI();
        }

        private void BtnConectar_Click(object sender, EventArgs e)
        {
            // Primero intento parsear el texto ingreasado a txtPuerto.
            // Si no es un entero muestro un mensaje de error y no hago nada más.
            if (int.TryParse(txtPuerto.Text, out int puerto))
            {
                // Obtengo la IP ingresada en txtIP
                string ip = txtIP.Text;

                // Me conecto con los datos ingresados
                cliente.Conectar(ip, puerto);
                Log($"El cliente se conectó al servidor IP = {cliente.RemoteEndPoint.Address}, Puerto = {cliente.RemoteEndPoint.Port}");

                // Actualizo la GUI
                UpdateUI();
            }
            else
            {
                MessageBox.Show("El puerto ingresado no es válido", Text);
            }
        }

        private void BtnEnviarMensaje_Click(object sender, EventArgs e)
        {
            // Envío lo que está escrito en la caja de texto del mensaje
            string destinatario = comboBoxDestinatarios.SelectedItem.ToString();
            var mensaje = txtMensaje.Text;
            string mensajeAEnviar = destinatario == "Todos" ? $"ALL:{mensaje}" : $"{destinatario}:{mensaje}";
            cliente.EnviarDatos(mensajeAEnviar);
            txtMensaje.Clear();
        }

        private void UpdateUI()
        {
            // Como este método se llama desde threads distintos al de la GUI 
            // necesitamos usar Invoke para poder acceder a los controles del form.
            Invoke((Action)delegate
            {
                // Habilito la posiblidad de conexión si el cliente está desconectado
                txtIP.Enabled = txtPuerto.Enabled = btnConectar.Enabled = !cliente.Conectado;

                // Habilito la posibilidad de enviar mensajes si el cliente está conectado
                txtMensaje.Enabled = btnEnviarMensaje.Enabled = cliente.Conectado;

                Text = cliente.Conectado ? $"Cliente (IP = {cliente.LocalEndPoint.Address}, Puerto = {cliente.LocalEndPoint.Port})" : "Cliente";
            });
        }
    }
}