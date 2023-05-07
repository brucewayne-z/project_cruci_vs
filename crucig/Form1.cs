using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace crucig
{
    public partial class Form1 : Form
    {
        private int partida_timerContador;
        private int turno_timerContador;
        private int restanteTimerContador;
        private int puntosPorPalabraJugador1 = 0;
        private int puntosPorPalabraJugador2 = 0;
        private int tiempoQue_le_quedaEnTurno;

        private string name_1player;
        private string name_2player;
        private string imagen1_src;
        private string imagen2_src;

        private bool seleccionadaPerfilFoto1 = false;
        private bool selecciondaPerfilFoto2 = false;
        private bool leTocaAJugador1 = true;
        private bool eventoEliminar_delete = false;

        private List<Tuple<int, int, char>> palabrasAGenerarParaComparar;
        private List<string> palabrasCorrectas;
        private List<string> acertadas_palabrasJuego;


        /* 
            Con el uso de tuplas se acumularán varios valores en una única entidad
            lo que hará nada eficiente cuando intentemos añadir más de 5 palabras, ya que
            se generarán varias tuplas y el proceso del cálculo se hace más pesado.
            Mi solución fue crear esta clase palabraEnObjeto, que guarda la estructura 
            principal de la extracción de las palabras, entonces esta clase representa cada
            palabra en el crucigrama, y no hay nada mejor que usar objetos para la 
            manipulación tan extensa de datos. Con esta clase manipulo toda la información
            de cada palabra de manera eficiente.
         */
        class palabraEnObjeto
        {
            public string palabraTextoExtraido { get; set; }
            public int eje_X { get; set; }
            public int eje_Y { get; set; }
            public int sizeDePalabra { get; set; }
            public char direccionDelaPalabra { get; set; }
            public string que_es_la_palabra { get; set; }

            public palabraEnObjeto(string palabraTextoExtraido,
                int eje_X, int eje_Y, int sizeDePalabra,
                char direccionDeLaPalabra,
                string que_es_la_palabra)
            {
                this.palabraTextoExtraido = palabraTextoExtraido;
                this.eje_X = eje_X;
                this.eje_Y = eje_Y;
                this.sizeDePalabra = sizeDePalabra;
                direccionDelaPalabra = direccionDeLaPalabra;
                this.que_es_la_palabra = que_es_la_palabra;
            }
        }

        private bool colocarPalabraIntento(palabraEnObjeto palabraIntentoAColocar, bool[,] tableroCrucigrama)
        {
            // Aplicando uso de ternarias en simples direccionamientos para ahorrar código

            int direccionX_de_H = palabraIntentoAColocar.direccionDelaPalabra == 'H' ? 1 : 0;
            int direccionY_de_Y = palabraIntentoAColocar.direccionDelaPalabra == 'V' ? 1 : 0;

            for (int i = 0; i < palabraIntentoAColocar.palabraTextoExtraido.Length; i++)
            {
                int x = palabraIntentoAColocar.eje_X + i * direccionX_de_H;
                int y = palabraIntentoAColocar.eje_Y + i * direccionY_de_Y;

                if (x < 0 || x >= tableroCrucigrama.GetLength(0) || y < 0 || y >= tableroCrucigrama.GetLength(1) || tableroCrucigrama[x, y])
                {
                    return false;
                }
            }

            for (int i = 0; i < palabraIntentoAColocar.palabraTextoExtraido.Length; i++)
            {
                int x = palabraIntentoAColocar.eje_X + i * direccionX_de_H;
                int y = palabraIntentoAColocar.eje_Y + i * direccionY_de_Y;
                tableroCrucigrama[x, y] = true;
            }

            return true;
        }

        private bool colocar_palabras_enCrucigrama(List<palabraEnObjeto> palabrasPorColocar, bool[,] tableroEnDondeSeColocal, int inicial_index = 0)
        {
            if (inicial_index == palabrasPorColocar.Count)
            {
                return true;
            }

            palabraEnObjeto palabra = palabrasPorColocar[inicial_index];

            for (int x = 0; x < tableroEnDondeSeColocal.GetLength(0); x++)
            {
                for (int y = 0; y < tableroEnDondeSeColocal.GetLength(1); y++)
                {
                    palabra.eje_X = x;
                    palabra.eje_Y = y;

                    if (colocarPalabraIntento(palabra, tableroEnDondeSeColocal))
                    {
                        if (colocar_palabras_enCrucigrama(palabrasPorColocar, tableroEnDondeSeColocal, inicial_index + 1))
                        {
                            return true;
                        }


                        /* Este siguiente bucle es muy importante para el funcionamiento
                           ya que se trata de un algoritmo de backtracking, que realiza un
                           proceso, que si falla o no es el deseado, deshace el proceso
                           y lo vuelve a realizar. En mi caso, estoy eliminando la palabra
                           que se colocó previamente y me permite colocar otra palabra
                           o ajustar la posición.
                           
                         */

                        for (int i = 0; i < palabra.palabraTextoExtraido.Length; i++)
                        {
                            int dx = palabra.direccionDelaPalabra == 'H' ? 1 : 0;
                            int dy = palabra.direccionDelaPalabra == 'V' ? 1 : 0;
                            tableroEnDondeSeColocal[palabra.eje_X + i * dx, palabra.eje_Y + i * dy] = false;
                        }
                    }
                }
            }

            return false;
        }
        private List<palabraEnObjeto> tomarPalabrasCualquiera(string rutaDelArchivoParaExtraer, int cantidadPalabras)
        {
            var rand = new Random();
            var leerDocumentoDeTexto = File.ReadAllLines(rutaDelArchivoParaExtraer);
            var palabrasAlmacenadas = new List<palabraEnObjeto>();
            var seSeleccionaronLineas = new HashSet<int>();
            while (seSeleccionaronLineas.Count < cantidadPalabras)
            {
                seSeleccionaronLineas.Add(rand.Next(leerDocumentoDeTexto.Length));
            }

            foreach (int indiceLinea in seSeleccionaronLineas)
            {
                var lineaAleatoria = leerDocumentoDeTexto[indiceLinea];
                var separarPorComas = lineaAleatoria.Split(',');
                for (int i = 0; i < separarPorComas.Length; i += 6)
                {
                    palabrasAlmacenadas.Add(new palabraEnObjeto(
                        separarPorComas[i],
                        int.Parse(separarPorComas[i + 1]),
                        int.Parse(separarPorComas[i + 2]),
                        int.Parse(separarPorComas[i + 3]),
                        separarPorComas[i + 4][0],
                        separarPorComas[i + 5]
                    ));
                }
            }

            return palabrasAlmacenadas;
        }

        private List<Control> iniciarCrucigrama(List<palabraEnObjeto> recibirPalabras)
        {
            palabrasAGenerarParaComparar = new List<Tuple<int, int, char>>();

            List<Control> controlesDeLosPaneles = new List<Control>();
            palabrasAGenerarParaComparar.Clear();

            int palabraInicial_numero = 1;
            int filaEnDondeSeEncuentra = 0;
            int columnaEnDondeSeEncuentra = 0;
            foreach (palabraEnObjeto palabra in recibirPalabras)
            {
                string textoObtenido = palabra.palabraTextoExtraido;
                int ejes_x = columnaEnDondeSeEncuentra * 2 + 1;
                int ejes_y = filaEnDondeSeEncuentra * 2 + 1;
                char direccion = palabraInicial_numero % 2 == 0 ? 'V' : 'H';

                Label numeroLabel = new Label();
                numeroLabel.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                numeroLabel.Location = new Point(ejes_x * 30 - 15, ejes_y * 30 + 40);
                numeroLabel.Name = $"labelNumero{palabraInicial_numero}";
                numeroLabel.Size = new Size(15, 15);
                numeroLabel.TabIndex = 0;
                numeroLabel.Text = palabraInicial_numero.ToString();
                numeroLabel.TextAlign = ContentAlignment.MiddleRight;
                numeroLabel.ForeColor = Color.White;
                controlesDeLosPaneles.Add(numeroLabel);

                for (int i = 0; i < textoObtenido.Length; i++)
                {
                    int posX = direccion == 'H' ? ejes_x + i : ejes_x;
                    int posY = direccion == 'H' ? ejes_y : ejes_y + i;

                    TextBox textBox = new TextBox();
                    textBox.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                    textBox.Location = new Point(posX * 30, posY * 30 + 35);
                    textBox.MaxLength = 1;
                    textBox.Name = $"textBox{ejes_x}_{ejes_y}_{direccion}{i}";
                    textBox.Size = new Size(30, 30);
                    textBox.TabIndex = 0;
                    textBox.TextAlign = HorizontalAlignment.Center;
                    textBox.ReadOnly = true; // Hacer que el TextBox sea de solo lectura
                    textBox.Text = "";
                    textBox.Tag = new Tuple<string, int>(textoObtenido, i);
                    controlesDeLosPaneles.Add(textBox);

                    palabrasAGenerarParaComparar.Add(new Tuple<int, int, char>(posX, posY, textoObtenido[i]));
                }
                palabrasCorrectas = extraccion_words(palabrasAGenerarParaComparar);
                palabraInicial_numero++;
                if (direccion == 'H')
                {
                    filaEnDondeSeEncuentra++;
                }
                else
                {
                    columnaEnDondeSeEncuentra++;
                }
            }

            return controlesDeLosPaneles;
        }



        private void pasarDeTurno()
        {
            contadorIntervaloPartida.Stop();
            resolvioPalabra_jugador = false;
            textPalabra.Clear();
            btnCheck.Enabled = true;
            leTocaAJugador1 = !leTocaAJugador1;
            labelJugadorActual.Text = leTocaAJugador1 ? name_1player : name_2player;
            tiempoQue_le_quedaEnTurno = turno_timerContador;
            labelTiempoTurnoRestante.Text = TimeSpan.FromSeconds(turno_timerContador).ToString(@"mm\:ss");
            contadorIntervaloPartida.Start();
        }


        private void timerTurno_Tick(object sender, EventArgs e)
        {
            if (eventoEliminar_delete)
            {
                return;
            }
            eventoEliminar_delete = true;
            tiempoQue_le_quedaEnTurno--;


            labelTiempoTurnoRestante.Text = TimeSpan.FromSeconds(tiempoQue_le_quedaEnTurno).ToString(@"mm\:ss");

            if (tiempoQue_le_quedaEnTurno == 0)
            {
                pasarDeTurno();
            }
            if (tiempoQue_le_quedaEnTurno > 0)
            {
                labelTiempoTurnoRestante.Text = TimeSpan.FromSeconds(tiempoQue_le_quedaEnTurno).ToString(@"mm\:ss");
                contadorIntervaloPartida.Start();
            }
            eventoEliminar_delete = false;
        }

        private void ActualizarPuntuacion(bool esJugador1, int puntos)
        {
            if (esJugador1)
            {
                puntosPorPalabraJugador1 += puntos;
                labelPuntuacionJugador1.Text = $"{puntosPorPalabraJugador1}";
            }
            else
            {
                puntosPorPalabraJugador2 += puntos;
                labelPuntuacionJugador2.Text = $"{puntosPorPalabraJugador2}";
            }
        }
        public Form1()
        {
            InitializeComponent();
            acertadas_palabrasJuego = new List<string>();
            btnCheck.Click += BtnCheck_Click;
            partida_timerContador = tiempoPartida_default;
            turno_timerContador = tiempoTurno_default;
            no.Enabled = false;

            contadorIntervaloPartida.Interval = 1000;
            timer1.Interval = 1000;
            contadorIntervaloPartida.Tick += timerTurno_Tick;
            palabrasAGenerarParaComparar = new List<Tuple<int, int, char>>();
            restanteTimerContador = partida_timerContador;
        }
        private List<string> extraccion_words(List<Tuple<int, int, char>> respuestasCorrectas)
        {
            List<string> palabrasCorrectas = new List<string>();
            StringBuilder palabra = new StringBuilder();

            for (int i = 0; i < respuestasCorrectas.Count; i++)
            {
                palabra.Append(respuestasCorrectas[i].Item3);

                bool nuevaPalabra = false;

                if (i < respuestasCorrectas.Count - 1)
                {
                    int dx = respuestasCorrectas[i + 1].Item1 - respuestasCorrectas[i].Item1;
                    int dy = respuestasCorrectas[i + 1].Item2 - respuestasCorrectas[i].Item2;
                    nuevaPalabra = (dx != 0 && dy != 0) || (dx > 1 || dy > 1);
                }

                if (i == respuestasCorrectas.Count - 1 || nuevaPalabra)
                {
                    palabrasCorrectas.Add(palabra.ToString());
                    palabra.Clear();
                }
            }

            return palabrasCorrectas;
        }

        private void colocarPalabraCorrespondiente(string palabra)
        {
            foreach (Control control in panelJuego.Controls)
            {
                if (control is TextBox textBox)
                {
                    Tuple<string, int> tag = textBox.Tag as Tuple<string, int>;
                    if (tag != null && tag.Item1 == palabra)
                    {
                        textBox.Text = tag.Item1[tag.Item2].ToString();
                        textBox.BackColor = Color.MediumSlateBlue;
                    }
                }
            }
        }

        private void btnTerminarTurno_Click(object sender, EventArgs e)
        {

            pasarDeTurno();
        }
        bool resolvioPalabra_jugador = false;

        private bool VerificarPalabras()
        {
            int tiempoTotalPartida = partida_timerContador - restanteTimerContador;
            // Obtener la palabra
            string palabraIngresada = textPalabra.Text.ToUpper();

            if (acertadas_palabrasJuego.Contains(palabraIngresada))
            {
                MessageBox.Show("Palabra ya existente.");
                return false;
            }
            else if (palabrasCorrectas.Contains(palabraIngresada))
            {
                acertadas_palabrasJuego.Add(palabraIngresada);
                colocarPalabraCorrespondiente(palabraIngresada);

            }
            else
            {
                return false;
            }

            List<string> palabrasCorrectasMinusculas = palabrasCorrectas.ConvertAll(p => p.ToLower());


            bool esCorrecta = palabrasCorrectas.Contains(palabraIngresada);

            Debug.WriteLine(palabrasCorrectas);

            btnCheck.Enabled = !esCorrecta;

            return esCorrecta;
        }

        private void palabrasTerminadas()
        {
            if (acertadas_palabrasJuego.Count == palabrasCorrectas.Count)
            {
                MessageBox.Show("Gracias por jugar mi juego!");
                TerminarPartida();
            }
        }

        private void BtnCheck_Click(object sender, EventArgs e)
        {
            if (!resolvioPalabra_jugador && VerificarPalabras())
            {
                MessageBox.Show("Palabra Correcta");

                ActualizarPuntuacion(leTocaAJugador1, 1);
                resolvioPalabra_jugador = true;
                palabrasTerminadas();
                pasarDeTurno();
            }
            else if (resolvioPalabra_jugador)
            {
                MessageBox.Show("Ya se ha resuelto.");
                palabrasTerminadas();
            }
            else
            {
                textPalabra.Clear();
                MessageBox.Show("Palabra incorrecta");
                palabrasTerminadas();
            }
        }

        private void btnGuardarConfiguracion_Click(object sender, EventArgs e)
        {
            partida_timerContador = (int)tiempoPartida.Value;
            turno_timerContador = (int)tiempoTurno.Value;

          
            if (partida_timerContador == 0)
            {
                partida_timerContador = tiempoPartida_default;
            }
            if (turno_timerContador == 0)
            {
                turno_timerContador = tiempoTurno_default;
            }

            Regex regex = new Regex("^[a-zA-Z0-9]*$");

            if (!regex.IsMatch(nameJugador1.Text) || !regex.IsMatch(nameJugador2.Text))
            {
                MessageBox.Show("El nombre de usuario solo debe contener letras o números.");
                return;
            }

            List<string> palabrasProhibidas = new List<string> { "palabrasobscenas", "tonto", "payaso"};
            foreach (string palabra in palabrasProhibidas)
            {
                if (nameJugador1.Text.Contains(palabra) || nameJugador2.Text.Contains(palabra))
                {
                    MessageBox.Show("No se permiten nombres iinadecuados.");
                    return;
                }
            }

            name_1player = nameJugador1.Text;
            name_2player = nameJugador2.Text;



          
            if (string.IsNullOrWhiteSpace(name_1player) || string.IsNullOrWhiteSpace(name_2player))
            {
                MessageBox.Show("Por favor, ingrese nombres válidos para ambos jugadores.");
                return;
            }
            if (seleccionadaPerfilFoto1 && selecciondaPerfilFoto2)
            {
                no.Enabled = true;
            }
            else
            {
                MessageBox.Show("Por favor seleccione las fotos de perfil ");
                return;
            }
        }


        private void btnFoto1_Click(object sender, EventArgs e)
        {
            if (openFile1.ShowDialog() == DialogResult.OK)
            {
               
                imagen1_src = openFile1.FileName;
                seleccionadaPerfilFoto1 = true;
            }
        }

        private void btnFoto2_Click(object sender, EventArgs e)
        {
            if (openFile1.ShowDialog() == DialogResult.OK)
            {
                
                imagen2_src = openFile1.FileName;
                selecciondaPerfilFoto2 = true;
            }
        }



        int tiempoPartida_default = 300;
        int tiempoTurno_default = 90;



        private void timer1_Tick(object sender, EventArgs e)
        {

            restanteTimerContador--;

            labelTiempoRestante.Text = TimeSpan.FromSeconds(restanteTimerContador).ToString(@"mm\:ss");

            // Comprobar si el tiempo de partida fue suficiente
            if (restanteTimerContador <= 0)
            {
                timer1.Stop();
                contadorIntervaloPartida.Stop();
                TerminarPartida();
            }
        }

        private void ReiniciarPartida()
        {
            puntosPorPalabraJugador1 = 0;
            puntosPorPalabraJugador2 = 0;
            labelPuntuacionJugador1.Text = "0";
            labelPuntuacionJugador2.Text = "0";

            EliminarCrucigrama();
            IniciarPartida();
        }



        private void ReiniciarConfiguraciones()
        {
            puntosPorPalabraJugador1 = 0;
            puntosPorPalabraJugador2 = 0;
            labelPuntuacionJugador1.Text = "0";
            labelPuntuacionJugador2.Text = "0";

            leTocaAJugador1 = false;
            labelJugadorActual.Text = name_1player;

            nameJugador1.Clear();
            nameJugador2.Clear();
            tiempoTurno.Value = 0;
            tiempoPartida.Value = 0;

            seleccionadaPerfilFoto1 = false;
            selecciondaPerfilFoto2 = false;

        }

        private void btnConfiguracion_Click(object sender, EventArgs e)
        {
            panelConfiguracion.Visible = true;
            panelJuego.Visible = false;
            panelResultados.Visible = false;
        }


        private void btnResultados_Click(object sender, EventArgs e)
        {
            panelConfiguracion.Visible = false;
            panelJuego.Visible = false;
            panelResultados.Visible = true;
        }
        private void TerminarPartida()
        {
            timer1.Stop();
            contadorIntervaloPartida.Stop();

            MostrarResultados();

            EliminarCrucigrama();
            ReiniciarConfiguraciones();
        }
        private string esGanadorOPerdedorOEmpate(int puntuacionJugador, int puntuacionOponente)
        {
            if (puntuacionJugador > puntuacionOponente)
            {
                return "Ganador";
            }
            else if (puntuacionJugador < puntuacionOponente)
            {
                return "Perdedor";
            }
            else
            {
                return "Empate";
            }
        }



        private Dictionary<string, Tuple<int, string, string>> resultados = new Dictionary<string, Tuple<int, string, string>>();


        private void MostrarResultados()
        {
            string jugadorStatus1 = esGanadorOPerdedorOEmpate(puntosPorPalabraJugador1, puntosPorPalabraJugador2);
            string jugadorStatus2 = esGanadorOPerdedorOEmpate(puntosPorPalabraJugador2, puntosPorPalabraJugador1);
            int tiempoDeLaPartidaTotal = partida_timerContador - restanteTimerContador;
            string mostrarElTiempoTotalFormateado = TimeSpan.FromSeconds(tiempoDeLaPartidaTotal).ToString(@"mm\:ss");

            if (name_1player == name_2player)
            {
                name_1player += " j1";
                name_2player += " j2";
            }

            if (resultados.ContainsKey(name_1player))
            {
                resultados[name_1player] = new Tuple<int, string, string>(puntosPorPalabraJugador1, mostrarElTiempoTotalFormateado, jugadorStatus1);
            }
            else
            {
                resultados.Add(name_1player, new Tuple<int, string, string>(puntosPorPalabraJugador1, mostrarElTiempoTotalFormateado, jugadorStatus1));
            }

            if (resultados.ContainsKey(name_2player))
            {
                resultados[name_2player] = new Tuple<int, string, string>(puntosPorPalabraJugador2, mostrarElTiempoTotalFormateado, jugadorStatus2);
            }
            else
            {
                resultados.Add(name_2player, new Tuple<int, string, string>(puntosPorPalabraJugador2, mostrarElTiempoTotalFormateado, jugadorStatus2));
            }

            var listaResultados = resultados.Select(r => new
            {
                Jugador = r.Key,
                PunteoTotal = r.Value.Item1,
                TiempoTotal = r.Value.Item2,
                Estado = r.Value.Item3
            }).ToList();

            dgvResultados.DataSource = listaResultados;

            if (!dgvResultados.Columns.Contains("Estado"))
            {
                DataGridViewTextBoxColumn columnaEstado = new DataGridViewTextBoxColumn
                {
                    Name = "Estado",
                    HeaderText = "Estado",
                    DataPropertyName = "Estado"
                };
                dgvResultados.Columns.Add(columnaEstado);
            }

            panelResultados.Visible = true;
            panelJuego.Visible = false;
            panelConfiguracion.Visible = false;
        }

        private void btnJugarPartida_Click_1(object sender, EventArgs e)
        {
            IniciarPartida();
        }

        private void btnEndGame_Click(object sender, EventArgs e)
        {
            TerminarPartida();
        }

        private void btnRegresar_Click(object sender, EventArgs e)
        {
            panelConfiguracion.Visible = false;
            panelJuego.Visible = false;
            panelResultados.Visible = false;
            panelPrincipal.Visible = false;
            btnResultados.Enabled = true;
            panelMenuPrincipal.Visible = true;
        }
        private void IniciarPartida()
        {
            restanteTimerContador = partida_timerContador;
            puntosPorPalabraJugador1 = 0;
            puntosPorPalabraJugador2 = 0;
            labelEstadisticasPlayer1.Text = nameJugador1.Text;
            labelEstadisticasPlayer2.Text = nameJugador2.Text;
            pictureJugador1.Image = Image.FromFile(imagen1_src);
            pictureJugador2.Image = Image.FromFile(imagen2_src);

            labelTiempoRestante.Text = TimeSpan.FromSeconds(partida_timerContador).ToString(@"mm\:ss");
            timer1.Start();

            tiempoQue_le_quedaEnTurno = turno_timerContador;
            labelTiempoTurnoRestante.Text = TimeSpan.FromSeconds(tiempoQue_le_quedaEnTurno).ToString(@"mm\:ss");

            contadorIntervaloPartida.Start();
            leTocaAJugador1 = true;
            labelJugadorActual.Text = name_1player;

            int palabrasA_aparecer = 5;
            //Nota.  por favor cambiar la ruta con el archivo de palabras para que funcione 
            // la extracción
            var palabrasGeneradas = tomarPalabrasCualquiera("coloca/la/ruta/de/palabrasCrucigrama//extraercrucigrama.txt", palabrasA_aparecer);

            for (int i = 0; i < palabrasA_aparecer; i++)
            {
                var palabra = palabrasGeneradas[i];
                var numeroLabel = panelJuego.Controls.Find($"n{i + 1}", true).FirstOrDefault() as Label;
                if (numeroLabel != null)
                {
                    numeroLabel.Text = $"{i + 1}.";
                }
                var definicionLabel = panelJuego.Controls.Find($"definicion{i + 1}", true).FirstOrDefault() as Label;

                if (definicionLabel != null)
                {
                    definicionLabel.Text = palabra.que_es_la_palabra;
                }
            }

            var controlesCrucigrama = iniciarCrucigrama(palabrasGeneradas);
            foreach (Control control in panelJuego.Controls.OfType<TextBox>().ToList())
            {
                if (control.Name.StartsWith("textBox"))
                {
                    panelJuego.Controls.Remove(control);
                }
            }


            foreach (Control control in controlesCrucigrama)
            {
                panelJuego.Controls.Add(control);
            }

            panelConfiguracion.Visible = false;
            panelJuego.Visible = true;
            btnResultados.Enabled = false;
            no.Enabled = false;
        }


        private void btnReiniciarPartida_Click(object sender, EventArgs e)
        {
            ReiniciarPartida();

        }

        private void btnJugarAhoraMenuPrincipal_Click(object sender, EventArgs e)
        {
            panelMenuPrincipal.Visible = false;
            panelJuego.Visible = false;
            panelResultados.Visible = false;
            panelPrincipal.Visible = true;
            panelConfiguracion.Visible = true;
        }
        private void EliminarCrucigrama()
        {
            // Eliminar controles de crucigrama (TextBox y Label con números) del panel del juego
            foreach (Control control in panelJuego.Controls.OfType<TextBox>().ToList())
            {
                if (control.Name.StartsWith("textBox"))
                {
                    panelJuego.Controls.Remove(control);
                }
            }

            foreach (Control control in panelJuego.Controls.OfType<Label>().ToList())
            {
                if (control.Name.StartsWith("labelNumero"))
                {
                    panelJuego.Controls.Remove(control);
                }
            }

            palabrasAGenerarParaComparar.Clear();
            palabrasCorrectas.Clear();
            acertadas_palabrasJuego.Clear();
        }

    }
}