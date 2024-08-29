using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SimulacionExclusionMutua
{
    public class FormularioPintores : Form
    {
        private Panel panelSuperior;
        private CheckedListBox listaPintores;
        private Label lblEstado;
        private Button[,] celdas;
        private int filas = 5;
        private int columnas = 5;
        private bool exclusionMutua = true;
        private Random random = new Random();
        private SemaphoreSlim semaforoBinario = new SemaphoreSlim(1, 1); // Semáforo binario
        private Color[] colores = { Color.LightBlue, Color.LightGreen, Color.LightYellow, Color.LightSlateGray };

        public FormularioPintores()
        {
            InicializarComponentes();
            InicializarCuadricula();
        }

        private void InicializarComponentes()
        {
            this.Text = "Simulación de Exclusión Mutua - Pintores Colaborativos";
            this.Size = new Size(800, 600);

            // Panel superior ajustado para que no cubra la cuadrícula
            panelSuperior = new Panel
            {
                Size = new Size(800, 50),
                Location = new Point(0, 0),
                BackColor = Color.LightYellow
            };

            lblEstado = new Label
            {
                Text = "Modo con Exclusión Mutua activado. Haz clic en las celdas para pintarlas.",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            panelSuperior.Controls.Add(lblEstado);
            this.Controls.Add(panelSuperior);

            // Lista de pintores
            listaPintores = new CheckedListBox
            {
                Location = new Point(620, 60),
                Size = new Size(150, 200)
            };

            for (int i = 1; i <= 4; i++)
            {
                listaPintores.Items.Add("Pintor " + i);
            }
            this.Controls.Add(listaPintores);

            // Botones para cambiar entre modos
            Button btnExclusionMutua = new Button
            {
                Text = "Modo con Exclusión Mutua",
                BackColor = Color.LightGreen,
                Location = new Point(150, 400),
                Size = new Size(200, 40)
            };
            btnExclusionMutua.Click += (sender, e) =>
            {
                exclusionMutua = true;
                lblEstado.Text = "Modo con Exclusión Mutua activado. Haz clic en las celdas para pintarlas.";
                lblEstado.BackColor = Color.LightYellow;
            };
            this.Controls.Add(btnExclusionMutua);

            Button btnSinExclusionMutua = new Button
            {
                Text = "Modo sin Exclusión Mutua",
                BackColor = Color.Red,
                Location = new Point(400, 400),
                Size = new Size(200, 40)
            };
            btnSinExclusionMutua.Click += (sender, e) =>
            {
                exclusionMutua = false;
                lblEstado.Text = "Modo sin Exclusión Mutua activado. Haz clic en las celdas para pintarlas.";
                lblEstado.BackColor = Color.LightCoral;
            };
            this.Controls.Add(btnSinExclusionMutua);
        }

        private void InicializarCuadricula()
        {
            celdas = new Button[filas, columnas];

            for (int i = 0; i < filas; i++)
            {
                for (int j = 0; j < columnas; j++)
                {
                    // Crear botones de las celdas
                    Button botonCelda = new Button
                    {
                        Size = new Size(60, 60),
                        Location = new Point(70 * j + 50, 70 * i + 50),
                        Text = $"[{i},{j}]",
                        BackColor = DefaultBackColor // Asegúrate de que el color de fondo sea el predeterminado
                    };
                    botonCelda.Click += new EventHandler(Celda_Click);
                    celdas[i, j] = botonCelda;
                    Controls.Add(botonCelda);
                }
            }
        }

        private async void Celda_Click(object sender, EventArgs e)
        {
            Button celdaSeleccionada = (Button)sender;
            var coordenadas = ExtraerCoordenadas(celdaSeleccionada.Text);
            int fila = coordenadas.Item1;
            int columna = coordenadas.Item2;

            if (listaPintores.CheckedItems.Count == 0)
            {
                MessageBox.Show("Selecciona al menos un pintor.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Obtener los pintores seleccionados actualmente
            var pintoresSeleccionados = listaPintores.CheckedItems;

            if (exclusionMutua)
            {
                // Modo con exclusión mutua
                await PintarCeldaConExclusion(celdaSeleccionada, pintoresSeleccionados, fila, columna);
            }
            else
            {
                // Modo sin exclusión mutua
                PintarCeldaSinExclusion(celdaSeleccionada, pintoresSeleccionados, fila, columna);
            }
        }

        private (int, int) ExtraerCoordenadas(string texto)
        {
            var regex = new Regex(@"\[(\d+),(\d+)\]");
            var match = regex.Match(texto);

            if (match.Success)
            {
                int fila = int.Parse(match.Groups[1].Value);
                int columna = int.Parse(match.Groups[2].Value);
                return (fila, columna);
            }

            throw new FormatException("Formato de coordenadas no válido.");
        }

        private async Task PintarCeldaConExclusion(Button celda, CheckedListBox.CheckedItemCollection pintores, int fila, int columna)
        {
            await semaforoBinario.WaitAsync(); // Esperar a obtener el semáforo

            try
            {
                foreach (var pintor in pintores)
                {
                    if (celda.BackColor == DefaultBackColor) // Celda no pintada
                    {
                        celda.BackColor = colores[listaPintores.Items.IndexOf(pintor)];
                        celda.Text = $"{pintor} [{fila},{columna}]";
                    }
                    else
                    {
                        lblEstado.Text = $"Conflicto: Celda [{fila},{columna}] ya está ocupada por {celda.Text}";
                        lblEstado.BackColor = Color.LightPink;
                    }
                }
            }
            finally
            {
                semaforoBinario.Release(); // Liberar el semáforo
            }
        }

        private void PintarCeldaSinExclusion(Button celda, CheckedListBox.CheckedItemCollection pintores, int fila, int columna)
        {
            // Simular diferentes tipos de conflictos
            foreach (var pintor in pintores)
            {
                if (celda.BackColor != DefaultBackColor)
                {
                    if (random.Next(0, 10) < 2) // 20% de probabilidad de interbloqueo
                    {
                        lblEstado.Text = "Interbloqueo detectado: Varios pintores intentan pintar la misma celda.";
                        lblEstado.BackColor = Color.Red;
                    }
                    else if (random.Next(0, 10) < 3) // 30% de probabilidad de inanición
                    {
                        lblEstado.Text = "Inanición: Un pintor no puede acceder a la celda debido a otros pintores.";
                        lblEstado.BackColor = Color.Orange;
                    }
                    else
                    {
                        lblEstado.Text = $"Conflicto: Celda [{fila},{columna}] ya está ocupada.";
                        lblEstado.BackColor = Color.Red;
                    }
                }
                else
                {
                    celda.BackColor = colores[listaPintores.Items.IndexOf(pintor)];
                    celda.Text = $"{pintor} [{fila},{columna}]";
                    lblEstado.Text = "Pintado sin conflicto, pero sin garantías de exclusión mutua.";
                    lblEstado.BackColor = Color.LightYellow;
                }
            }
        }
    }

    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormularioPintores());
        }
    }
}
