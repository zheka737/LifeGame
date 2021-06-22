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
    /// Interaction logic for GameArea.xaml
    /// </summary>
    public partial class GameArea : Page, IPageOfMainWindow
    {
        public bool DrawAliveCellsMode { get; set; } = false;
        public List<Point> DrawedPoints = new List<Point>();
        private SolidColorBrush colourOfAliveCell = new SolidColorBrush(Colors.Green);
        private SolidColorBrush colourOfDeadCell = new SolidColorBrush(Colors.Black);

        public GameArea()
        {
            InitializeComponent();
        }

        public void Update()
        {
            //TODO Optimese do not use rectangles but draw objects
            Application.Current.Dispatcher.Invoke((Action)delegate {
                GameAreaCanvas.Children.Clear();

                // your code
                foreach (Cell cell in GameController.FetchGameController.Cells)
                {
                    Rectangle rect = new System.Windows.Shapes.Rectangle();
                    rect.Stroke = new SolidColorBrush(Colors.Black);
                    if (cell.Alive)
                    {
                        rect.Fill = colourOfAliveCell;
                    }
                    else
                    {
                        rect.Fill = colourOfDeadCell;
                    }
                    rect.Width = GameController.CellSize;
                    rect.Height = GameController.CellSize;
                    Canvas.SetLeft(rect, cell.PositionX);
                    Canvas.SetTop(rect, cell.PositionY);
                    rect.MouseEnter += Rectangle_MouseEnter;

                    GameAreaCanvas.Children.Add(rect);
                };
            });
        }

        private void Rectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            if (DrawAliveCellsMode)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Rectangle currentRect = sender as Rectangle;

                    if (currentRect == null)
                    {
                        throw new Exception();
                    }

                    Point pointOfCell = new Point(Canvas.GetLeft(currentRect), Canvas.GetTop(currentRect));

                    if (!DrawedPoints.Contains(pointOfCell))
                    {
                        currentRect.Fill = colourOfAliveCell;
                        DrawedPoints.Add(pointOfCell);
                    }
                }
            }
        }


    }
}
