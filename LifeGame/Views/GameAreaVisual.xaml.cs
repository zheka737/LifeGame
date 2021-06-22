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
using System.Windows.Navigation;
using System.Windows.Shapes;
using LifeGame.Controllers;
using LifeGame.Model;

namespace LifeGame.Views
{
    /// <summary>
    /// Interaction logic for GameAreaVisual.xaml
    /// </summary>
    public partial class GameAreaVisual : Page, IPageOfMainWindow
    {
        private SolidColorBrush colourOfAliveCell = new SolidColorBrush(Colors.Green);
        private SolidColorBrush colourOfDeadCell = new SolidColorBrush(Colors.Black);
        public GameAreaVisual()
        {
            InitializeComponent();
        }

        public void Update()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {

                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    foreach (Cell cell in GameController.FetchGameController.Cells)
                    {
                        SolidColorBrush colour = null;

                        if (cell.Alive)
                        {
                            colour = colourOfAliveCell;
                        }
                        else
                        {
                            colour = colourOfDeadCell;
                        }

                        drawingContext.DrawRectangle(colour, new Pen(Brushes.Black, 1), new Rect(new Point(cell.PositionX, cell.PositionY), 
                            new Size(GameController.CellSize, GameController.CellSize)));
                    }

                    //drawingContext.DrawRoundedRectangle(Brushes.Yellow, new Pen(Brushes.Black, 5),
                    //    new Rect(5, 5, 450, 100), 20, 20);
                }
                RenderTargetBitmap bmp = new RenderTargetBitmap(GameController.GameAreaSizeX, GameController.GameAreaSizeY, 100, 100, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);

                GameAreaImage.Source = bmp;

            });
        }
    }
}
