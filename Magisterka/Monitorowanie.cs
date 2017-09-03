using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video.DirectShow;
using System.Drawing.Imaging;
using System.Threading;
using System.Runtime.CompilerServices;
using CsvHelper;
using System.IO;
using System.Collections.Generic;

namespace main
{
    public partial class Monitorowanie : Form
    {
        static System.Timers.Timer odczekanieNaOstrosc=new System.Timers.Timer();
        static int odliczDo5 = 5;
        static string aktualnyStanWykresu = "statyczny";
        static bool czyKolorWybrany = false;
        static int licznikResetu = 0;
        static int x = 0;
        private FilterInfoCollection videoDevices;
        EuclideanColorFiltering filter = new EuclideanColorFiltering();
        Color kolor = Color.White;
        Color kolorTla = Color.FromArgb(248,248,255);
        GrayscaleBT709 filtrSzarosci = new GrayscaleBT709();
        BlobCounter licznikDrobin = new BlobCounter(); 
        int range = 120;
        bool pierwszyRazPoResecie = false;
      public static int[] liczniki = new int[10000];                
       public static RozmiaryFigury[] tablicaFigur = new RozmiaryFigury[10000];
        public Monitorowanie()
        {
            InitializeComponent();
            licznikDrobin.MinWidth = 1; //minimalny rozmiar obiektu = 1 pixel
            licznikDrobin.MinHeight = 1;
            licznikDrobin.FilterBlobs = true;
            licznikDrobin.ObjectsOrder = ObjectsOrder.Size;
            odczekanieNaOstrosc.AutoReset = true;
            odczekanieNaOstrosc.Interval = 1000;
            odczekanieNaOstrosc.Elapsed += OdczekanieNaOstrosc_Elapsed;        
            try
            {
                // tworzenie listy dostępnych urządzeń wideo
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0)
                {
                    camerasCombo.Items.Add("Podłącz urządzenie");
                    videoDevices = null;
                }
                // dodaj dostępne urządzenia do listy
                foreach (FilterInfo device in videoDevices)
                {
                    camerasCombo.Items.Add(device.Name);
                }
                camerasCombo.SelectedIndex = 0;
            }
            catch(Exception)
            { }                     
        }

        private void OdczekanieNaOstrosc_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                button3.Text = "Ustawianie ostrości obrazu..." + odliczDo5;
                odliczDo5--;
                if (odliczDo5 == -1)  //opoźnienie wynikające z delegaty, aby uniknąć wyświetlania 0 w 3,2,1,0 dla uzytkownika 
                { button3.Enabled = true;
                    odczekanieNaOstrosc.Stop();
                    button3.Text = "Wybierz kolor";
                }
            });
        }

        private void videoSourcePlayer1_NewFrame(object sender, ref Bitmap obraz)
        {        
            int x = 0;           
            Bitmap objectsImage = null;
            Bitmap mImage = null;
            mImage=(Bitmap)obraz.Clone();            
           filter.CenterColor = Color.FromArgb(kolor.ToArgb());
            filter.Radius =(short)range;           
            objectsImage = obraz;
            filter.ApplyInPlace(objectsImage);
            if (czyKolorWybrany==true) {
                BitmapData objectsData = objectsImage.LockBits(new Rectangle(0, 0, obraz.Width, obraz.Height), ImageLockMode.ReadOnly, obraz.PixelFormat);
                UnmanagedImage grayImage = filtrSzarosci.Apply(new UnmanagedImage(objectsData));
                objectsImage.UnlockBits(objectsData);
                licznikDrobin.ProcessImage(grayImage);
                Rectangle[] kwadraty = licznikDrobin.GetObjectRectangles();
                if (kwadraty.Length > 0)
                {
                    foreach (Rectangle aktualnyKwadrat in kwadraty)
                    {
                        Graphics grafika = Graphics.FromImage(mImage);
                        using (Pen pen = new Pen(Color.FromArgb(0, 255, 0), 2))
                        {
                            grafika.DrawRectangle(pen, aktualnyKwadrat);
                        }
                        RozmiaryFigury a = new RozmiaryFigury(aktualnyKwadrat.Height, aktualnyKwadrat.Width);  //tutaj wypelniaja sie liczniki (wewnatrz klasy RozmiaryFigury)
                        tablicaFigur[x] = a;
                        x++;
                        grafika.Dispose();
                    }
                    if (aktualnyStanWykresu == "dynamiczny")
                    {
                        backgroundWorker1.RunWorkerAsync();
                    }
                    if(aktualnyStanWykresu=="statyczny" && licznikResetu<2)
                    {
                        backgroundWorker1.RunWorkerAsync();
                        licznikResetu++;
                        backgroundWorker1.CancelAsync();                       
                    }
                    if (aktualnyStanWykresu == "statyczny" && licznikResetu==2)
                    {                    
                        backgroundWorker1.CancelAsync();
                    }                 
                }
            }           
            obraz = mImage;
        }
        private void videoSourcePlayer3_NewFrame(object sender, ref Bitmap image)
        {
            Bitmap obrazObiektu = null;
            if (czyKolorWybrany == true)
            {
                // ustaw kolor
                filter.CenterColor = Color.FromArgb(kolor.ToArgb());
                filter.Radius = (short)range;
                // zastosuj filtr
                obrazObiektu = image;
                filter.ApplyInPlace(image);
                // zablokuj obraz do dalszego przetwarzania
                BitmapData objectsData = obrazObiektu.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly, image.PixelFormat);
                // wprowadzenie skali szarości
                UnmanagedImage grayImage = filtrSzarosci.Apply(new UnmanagedImage(objectsData));
                // odblokowanie danych obrazu
                obrazObiektu.UnlockBits(objectsData);
                // lokacja zarysów obiektu 
                licznikDrobin.ProcessImage(grayImage);
                Rectangle[] tablicaKwadratow = licznikDrobin.GetObjectRectangles();
                if (tablicaKwadratow.Length > 0)
                {
                    Rectangle pojedynczyKwadrat = tablicaKwadratow[0];
                    // obrysowanie obiektu kwadratem
                    Graphics g = Graphics.FromImage(image);
                    using (Pen pen = new Pen(Color.FromArgb(176, 23, 31), 5))
                    {
                        g.DrawRectangle(pen, pojedynczyKwadrat);
                    }
                    g.Dispose();                
                }
                Graphics g1 = Graphics.FromImage(image);             
                g1.Dispose();
            }
       }
          
        private void button1_Click(object sender, EventArgs e) //start
        {
            if(videoDevices==null)
            {
                MessageBox.Show("Program nie zadziała bez podłączonej kamery!");
                return;
            }
            videoSourcePlayer1.SignalToStop();
            videoSourcePlayer1.WaitForStop();
            videoSourcePlayer2.SignalToStop();
            videoSourcePlayer2.WaitForStop();
            videoSourcePlayer3.SignalToStop();
            videoSourcePlayer3.WaitForStop();           
            VideoCaptureDevice videoSource = new VideoCaptureDevice(videoDevices[camerasCombo.SelectedIndex].MonikerString);
            videoSource.DesiredFrameSize = new Size(320, 240); //rozmiar klatki powinien być równy obszarowi video
            videoSource.DesiredFrameRate = 30; //pozwala na określenie liczby analizowanych klatek, kamera użyta w pracy pozwala na rejestrowanie 30 klatek na sekundę, parametr powinno się zwiększyć dla kamer o innych parametrach
            videoSourcePlayer1.VideoSource = videoSource;
            videoSourcePlayer1.Start();
            videoSourcePlayer2.VideoSource = videoSource;
            videoSourcePlayer2.Start();
            videoSourcePlayer3.VideoSource = videoSource;
            videoSourcePlayer3.Start();
            if (odczekanieNaOstrosc.Enabled == false)
            {
                odczekanieNaOstrosc.Start();
                odliczDo5 = 5;
            }
            button3.Enabled = false; // dezaktywacja przycisku wyboru koloru
        }

        private void button2_Click(object sender, EventArgs e) //rozłącz
        {
            videoSourcePlayer1.SignalToStop();          
            videoSourcePlayer1.WaitForStop();
            videoSourcePlayer1.Stop();
            videoSourcePlayer1.VideoSource.Stop();         
            videoSourcePlayer2.SignalToStop();
            videoSourcePlayer2.WaitForStop();
            videoSourcePlayer2.VideoSource.Stop();
            videoSourcePlayer2.Stop();
            videoSourcePlayer3.SignalToStop();
            videoSourcePlayer3.WaitForStop();
            videoSourcePlayer3.Stop();
            videoSourcePlayer3.VideoSource.Stop();
            if (odczekanieNaOstrosc.Enabled == false)
            { odliczDo5 = 5; }
        }

        private void button3_Click(object sender, EventArgs e) //wybierz kolor
        {
            colorDialog1.ShowDialog();
            kolor = colorDialog1.Color;
            czyKolorWybrany = true;
            chart1.Visible = true;
        }
 
        private void button4_Click(object sender, EventArgs e)  //przycisk reset
        {
            Array.Clear(liczniki, 0, liczniki.Length); //zamiana wszystkich wartości w tablicy na 0
            Array.Clear(tablicaFigur, 0, tablicaFigur.Length);
            chart1.Series["Dane"].Points.Clear();
            backgroundWorker1.CancelAsync(); //zaprzestanie zliczania
            licznikResetu = 0;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)  //statyczny wykres  
        {
            aktualnyStanWykresu = "statyczny";
            Array.Clear(liczniki, 0, liczniki.Length);
            Array.Clear(tablicaFigur, 0, tablicaFigur.Length);
            chart1.Series["Dane"].Points.Clear();
            backgroundWorker1.CancelAsync();
            licznikResetu = 0;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e) //dynamiczny wykres
        {
            aktualnyStanWykresu = "dynamiczny";
            Array.Clear(liczniki, 0, liczniki.Length);
            Array.Clear(tablicaFigur, 0, tablicaFigur.Length);
            chart1.Series["Dane"].Points.Clear();
            backgroundWorker1.CancelAsync();
            licznikResetu = 0;
        }

        private void Monitorowanie_FormClosing(object sender, FormClosingEventArgs e)
        {
            videoSourcePlayer1.SignalToStop();
            videoSourcePlayer1.WaitForStop();
            videoSourcePlayer2.SignalToStop();
            videoSourcePlayer2.WaitForStop();
            videoSourcePlayer3.SignalToStop();
            videoSourcePlayer3.WaitForStop();
            groupBox1.Enabled = true;
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                for (int i = 0; i < liczniki.Length; i++)
                {
                    int k = liczniki[i];
                    if (liczniki[i] != 0)
                    {
                        /**
                         * Aby algorytm spełniał swoje zadanie, do obliczeń potrzebujemy dwóch klatek. Pierwszej jako wzorca, drugiej
                         * jako porównania. Efektem ubocznym jest podwojenie wyników, w przypadku używania zliczania statycznego, przez
                         * co dzielimy wynik przez 2 w takowym przypadku.
                         */
                        if (pierwszyRazPoResecie == true)
                        {
                            chart1.Series["Dane"].Points.AddXY(i, liczniki[i]);                   
                        }
                        else
                        {
                            chart1.Series["Dane"].Points.AddXY(i, liczniki[i] / 2); 
                        }                     
                        label12.Text = (liczniki[1] / 2).ToString();  //wpisanie wartości z wykresu do tabeli norm
                        label15.Text = (liczniki[2] / 2).ToString();
                        label16.Text = (liczniki[4] / 2).ToString();
                        label17.Text = (liczniki[6] / 2).ToString();
                        label18.Text = (liczniki[8] / 2).ToString();
                        label19.Text = (liczniki[10] / 2).ToString();
                        label20.Text = (liczniki[12] / 2).ToString();
                        label27.Text = (liczniki[14] / 2).ToString();
                        label28.Text = (liczniki[16] / 2).ToString();
                        label29.Text = (liczniki[20] / 2).ToString();
                        label30.Text = (liczniki[32] / 2).ToString();
                        label31.Text = (liczniki[40] / 2).ToString();
                        label32.Text = (liczniki[63] / 2).ToString();
                    }
                }
                x++; //todo comment
            });
            if (backgroundWorker1.CancellationPending)
            {
                e.Cancel = true;               
                return;
            }
        }

        private void button5_Click(object sender, EventArgs e) //eksport do CSV
        {
            saveFileDialog1.DefaultExt = "csv";
            saveFileDialog1.Filter = "Pliki CSV (*.csv)|*.csv";         
            saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); 
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var etykiety = new List<Label> { label40, label41, label42, label43, label44, label45, label46, label47, label48, label49, label50, label51, label52, label53 };
                var etykietyDanych = new List<Label> { label13, label12, label15, label16, label17, label18, label19, label20, label27, label28, label29, label30, label31, label32 };
                Label[] labels = etykiety.ToArray(); //modyfikacja do tablicy aby móc wpisywać dane w jednej interacji w pętli for, zamiast foreach
                Label[] labels1 = etykietyDanych.ToArray();
                string sciezka = saveFileDialog1.FileName;
                StreamWriter wpisz = new StreamWriter(sciezka, false, System.Text.Encoding.UTF8);
                var csv = new CsvWriter(wpisz);
                for (int i = 0; i < 13; i++) //liczba etykiet
                {
                    csv.WriteField(labels[i].Text); //kolumna z nazwami          
                    csv.WriteField(labels1[i].Text); //kolumna z danymi           
                    csv.NextRecord();
                }
                wpisz.Flush();
            }
        }

       

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            int i = 0;
              for(int k=0;k<10000;k++)
            {
                if (tablicaFigur[k] != null)
                    i++;
            }
                MessageBox.Show("Łączna liczba elementów to: "+i.ToString(),"Histogram");
            
        }
    }
}
