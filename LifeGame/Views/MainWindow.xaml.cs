using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
using LifeGame.Models;
using LifeGame.Views;

namespace LifeGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainWindow
    {
        private GameController GameController { get; }
        private GameArea GameAreaScreen { get; } 
        //private GameAreaVisual GameAreaVisual { get; }
        private ChooseSaveToLoad ChooseSaveToLoadScreen { get; }
        private SaveTheGame SaveTheGameScreen { get; }


        public MainWindow()
        {
            InitializeComponent();
            GameAreaScreen = new GameArea();
            //GameAreaVisual = new GameAreaVisual();
            GameController = new GameController(this);
            ChooseSaveToLoadScreen = new ChooseSaveToLoad();
            SaveTheGameScreen = new SaveTheGame();

            LifeGameWindow.DataContext = GameController;

            Update();
        }

        public void Update()
        {
            this.Dispatcher.Invoke(() =>
            {
                IPageOfMainWindow pageOfMainWindow = MainAreaFrame.Content as IPageOfMainWindow;
                pageOfMainWindow?.Update();
                if (GameController != null)
                {
                    //UseToroid.Content = GameController.UseToroid ? "Замкнутая" : "Ограниченная";
                    List<string> log = GameController.RetriveAllLog();
                    Log.Items.Clear();
                    foreach (string s in log)
                    {
                        Log.Items.Add(s);
                    }

                    foreach (object child in ButtomsArea.Children)
                    {
                        if (child is Button && ((Button) child).Name == "StartButton")
                        {
                            ((Button) child).IsEnabled = GameController.Cells.Any(cell => cell.Alive);
                        }
                    }
                }



            });


        }

        public void SwitchOffDrawAliveCellsState()
        {
            GameArea gameArea = MainAreaFrame.Content as GameArea;
            gameArea.DrawedPoints.Clear();
            gameArea.DrawAliveCellsMode = false;
        }

        public void SwitchToLoadScreen()
        {
            this.Dispatcher.Invoke(() =>
            {
                MainAreaFrame.Content = ChooseSaveToLoadScreen;
                ChooseSaveToLoadScreen.Update();
            });
        }

        public void SwitchToSaveScreen()
        {
            this.Dispatcher.Invoke(() =>
            {
                MainAreaFrame.Content = SaveTheGameScreen;
                SaveTheGameScreen.NameOfSaveBox.Text = "";
                SaveTheGameScreen.Update();
            });
        }

        public void SwitchOnDrawAliveCellsState()
        {
            GameArea gameArea = MainAreaFrame.Content as GameArea;
            gameArea.DrawedPoints.Clear();
            gameArea.DrawAliveCellsMode = true;
        }

        public void ClearButtomsArea()
        {
            this.Dispatcher.Invoke(() =>
            {
                ButtomsArea.Children.Clear();
            });
        }

 

        public void SwitchToGameAreaVisual()
        {
            this.Dispatcher.Invoke(() =>
            {
                //TODO check this
                MainAreaFrame.Content = GameAreaScreen;
                GameAreaScreen.Update();
            });



        }


        private void Next_OnClick(object sender, RoutedEventArgs e)
        {
            GameController.NextGeneration();
        }

        #region buttons
        public void AddStartButton()
        {
            Button startButton = ButtonForButtomsAreaFactory();
            startButton.Name = "StartButton";
            startButton.Content = "Старт";
            startButton.Command = new StartCommand();
            ButtomsArea.Children.Add(startButton);
        }

        public void AddLoadRandSaveButton()
        {
            Button loadRandSaveButton = ButtonForButtomsAreaFactory();
            loadRandSaveButton.Name = "LoadRandSaveButton";
            loadRandSaveButton.Content = new TextBlock() { Text = "Загрузить случайное сохранение", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center};
            loadRandSaveButton.Command = new LoadRandomCommand();
            ButtomsArea.Children.Add(loadRandSaveButton);
        }

        public void AddLoadSaveButton()
        {
            Button loadSaveButton = ButtonForButtomsAreaFactory();
            loadSaveButton.Name = "LoadSaveButton";
            loadSaveButton.Content = "Загрузить";
            loadSaveButton.Command = new LoadCommand();
            ButtomsArea.Children.Add(loadSaveButton);
        }

        public void AddNewButton()
        {
            Button newGameButton = ButtonForButtomsAreaFactory();
            newGameButton.Name = "NewGameButton";
            newGameButton.Content = "Новая игра";
            newGameButton.Command = new NewGameCommand();
            ButtomsArea.Children.Add(newGameButton);
        }

        public void AddPauseButton()
        {
            Button pauseButton = ButtonForButtomsAreaFactory();
            pauseButton.Name = "PauseButton";
            pauseButton.Content = "Пауза";
            pauseButton.Command = new PauseCommand();
            ButtomsArea.Children.Add(pauseButton);
        }

        public void AddSaveGameButton()
        {
            Button saveButton = ButtonForButtomsAreaFactory();
            saveButton.Name = "SaveButton";
            saveButton.Content = "Сохранить";
            saveButton.Command = new SaveCommand();
            ButtomsArea.Children.Add(saveButton);
        }

        public void AddExitButton(State returnState = null)
        {
            this.Dispatcher.Invoke(() =>
            {
                Button exitButton = ButtonForButtomsAreaFactory();
                exitButton.Name = "ExitButton";
                exitButton.Content = "Выйти";
                exitButton.Command = new ExitCommand();
                exitButton.CommandParameter = returnState;
                ButtomsArea.Children.Add(exitButton);
            });
        }

        public void AddContinueButton()
        {
            Button continueButton = ButtonForButtomsAreaFactory();
            continueButton.Name = "ContinueButton";
            continueButton.Content = "Продолжить";
            continueButton.Command = new GameCommand();
            ButtomsArea.Children.Add(continueButton);
        }

        public void AddApplyDrawButton()
        {
            Button applyDrawButton = ButtonForButtomsAreaFactory();
            applyDrawButton.Name = "ApplyDrawButton";
            applyDrawButton.Content = "Ок";
            applyDrawButton.Click += (sender, args) =>
            {
                GameArea gameArea = MainAreaFrame.Content as GameArea;
                GameController.FetchGameController.ApplyDrawedAliveCellsToModel(gameArea.DrawedPoints);
                gameArea.DrawedPoints.Clear();
                SwitchOffDrawAliveCellsState();
                GameController.State = new NewGameState();
            };
            ButtomsArea.Children.Add(applyDrawButton);
        }

        public void AddCancelDrawButton()
        {
            Button cancelDrawButton = ButtonForButtomsAreaFactory();
            cancelDrawButton.Name = "CancelDrawButton";
            cancelDrawButton.Content = "Отмена";
            cancelDrawButton.Click += (sender, args) =>
            {
                GameArea gameArea = MainAreaFrame.Content as GameArea;
                gameArea.DrawedPoints.Clear();
                SwitchOffDrawAliveCellsState();
                GameController.State = new NewGameState();
            };
            ButtomsArea.Children.Add(cancelDrawButton);
        }

        public void AddAutoGenerateAliveCellsButton()
        {
            Button autoGenAliveCellsButton = ButtonForButtomsAreaFactory();
            autoGenAliveCellsButton.Name = "AutoGenAliveCellsButton";
            autoGenAliveCellsButton.Content = new TextBlock() { Text = "Случаная расстановка", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
            autoGenAliveCellsButton.Command = new AutoGenAliveCellsCommand();
            ButtomsArea.Children.Add(autoGenAliveCellsButton);
        }

        public void AddDrawAliveCellsButton()
        {
            Button drawAliveCellsButton = ButtonForButtomsAreaFactory();
            drawAliveCellsButton.Name = "DrawAliveCellsButton";
            drawAliveCellsButton.Content = new TextBlock() { Text = "Режим рисования", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
            drawAliveCellsButton.Command = new DrawAliveCellsCommand();
            ButtomsArea.Children.Add(drawAliveCellsButton);
        }

        public void AddUseToroidToggle()
        {


            GroupBox groupBox = new GroupBox();
            groupBox.Header = "Тип вселенной:";


            RadioButton radioButton1 = new RadioButton();
            radioButton1.Name = "RadioButton1";
            radioButton1.Content = "Огранниченная";
            radioButton1.Checked += (sender, args) => { GameController.UseToroid = false; };
            radioButton1.IsChecked = true;

            RadioButton radioButton2 = new RadioButton();
            radioButton2.Name = "RadioButton2";
            radioButton2.Content = "Замкнутая";
            radioButton2.Checked += (sender, args) => { GameController.UseToroid = true; };

            StackPanel stackPanel = new StackPanel();
            stackPanel.Children.Add(radioButton1);
            stackPanel.Children.Add(radioButton2);
            groupBox.Content = stackPanel;


            ButtomsArea.Children.Add(groupBox);
        }

        public Button ButtonForButtomsAreaFactory()
        {
            Button button = new Button();
            button.Margin = new Thickness(5, 5, 5, 5);
            button.MinHeight = 30;
            button.MaxHeight = 80;
            return button;
        }


        #endregion

    }


    public static class DisableNavigation
    {
        public static bool GetDisable(DependencyObject o)
        {
            return (bool)o.GetValue(DisableProperty);
        }
        public static void SetDisable(DependencyObject o, bool value)
        {
            o.SetValue(DisableProperty, value);
        }

        public static readonly DependencyProperty DisableProperty =
            DependencyProperty.RegisterAttached("Disable", typeof(bool), typeof(DisableNavigation),
                new PropertyMetadata(false, DisableChanged));



        public static void DisableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var frame = (Frame)sender;
            frame.Navigated += DontNavigate;
            frame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
        }

        public static void DontNavigate(object sender, NavigationEventArgs e)
        {
            ((Frame)sender).NavigationService.RemoveBackEntry();
        }
    }


    public interface IPageOfMainWindow
    {
        void Update();
    }
}

