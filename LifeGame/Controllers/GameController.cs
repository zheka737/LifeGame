using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using LifeGame.Annotations;
using LifeGame.Model;
using LifeGame.Models;
using LifeGame.Views;

namespace LifeGame.Controllers
{
    public class GameController:INotifyPropertyChanged
    {
        private static State _state = null;
        private Guid _currentGuidOfLog;
        public static GameController FetchGameController { get; set; } = null;
        //Preferences //TODO bind to form
        public static int GameAreaSizeX { get; } = 300;
        public static int GameAreaSizeY { get; } = 300;
        public static int CellSize { get; } = 10;
        public GameModel GameModel { get; }

        public bool UseToroid
        {
            get { return GameModel.UseToroid; }
            set
            {
                GameModel.UseToroid = value;
            }
        }

        private IMainWindow MainWindow { get; }


        public Timer Timer { get; }=  new Timer(1000);

        public State State
        {
            get { return _state; }
            set
            {
                value.doAction(this, MainWindow);
                OnPropertyChanged("");
                _state = value;
            }
        }

        public List<Cell> Cells
        {
            get { return GameModel.Cells; }
        }

        public GameController(IMainWindow mainWindow)
        {
            
            MainWindow = mainWindow;

            if (FetchGameController != null)
            {
                throw new Exception("This class is singelton");
            }
            else
            {
                FetchGameController = this;
            }

            GameModel = new GameModel(GameAreaSizeX, GameAreaSizeY, CellSize, false);
            Timer.Elapsed += (sender, args) =>
            {
                NextGeneration();
            };
            GameModel.GameIsOver += (sender, args) =>
            {
                Timer.Stop();
                State = new GameOverState();
                //ResetModelFilledWithDeadCells();
                mainWindow.Update();
            };



            State = new StartState();

        }

        public void NextGeneration()
        {
            GameModel.NextGeneration();
            MainWindow.Update();
        }

        public void RandAutoPopulateCells()
        {
            GameModel.RandAutoPopulateCells();
            MainWindow.Update();
        }

        public void ApplyDrawedAliveCellsToModel(List<Point> drawedCells)
        {
            GameModel.Cells.Clear();

            foreach (Point drawedCell in drawedCells)
            {
                GameModel.AddCellToGameModelWithCheckOfOverlapping(new Cell((int)drawedCell.X, (int)drawedCell.Y)
                {
                    Alive = true
                });
            }

            GameModel.PopulateRemainedSpaceWithDeadCells();
        }

        public void ResetModelFilledWithDeadCells()
        {
            GameModel.ResetModelFilledWithDeadCells();
            MainWindow.Update();
        }

        public void SaveGame(Guid idOfSave, string nameOfSave)
        {
            GameModel.SaveGame(idOfSave, nameOfSave);
            State = new PauseState();
        }

        public void LoadGame(Guid idOfSave)
        {
            GameModel.LoadGame(idOfSave);
            State = new PauseState();
            MainWindow.Update();
        }

        public bool LoadRandSave()
        {
            bool result = GameModel.LoadRandomSave();
            MainWindow.Update();
            return result;
        }

        public static void DeleteSave(Guid idOfSave)
        {
            using (SqlConnection cn = WorkWithSaves.DbConnectionFactory())
            {
                SqlTransaction tx = null;
                try
                {
                    tx = cn.BeginTransaction();
                    WorkWithSaves.DeleteSave(idOfSave, cn, tx);
                    tx.Commit();
                }
                catch (Exception e)
                {
                    tx.Rollback();
                    throw;
                }
            }

           
        }

        public void WriteLogStartOfGame()
        {
           _currentGuidOfLog = GameModel.WriteLogStartOfGame();
        }

        public void WriteLogEndOfGame()
        {
            if(_currentGuidOfLog != Guid.Empty)
                GameModel.WriteLogEndOfGame(_currentGuidOfLog);
        }

        public List<string> RetriveAllLog()
        {
            return GameModel.RetrieveAllLog();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region State control
    public interface State
    {
        void doAction(GameController controller, IMainWindow mainWindow);
    }

    public class StartState:State
    {
        public void doAction(GameController controller, IMainWindow mainWindow)
        {
            mainWindow.SwitchToGameAreaVisual();
            mainWindow.ClearButtomsArea();
            mainWindow.AddNewButton();
            mainWindow.AddLoadSaveButton();
            mainWindow.AddLoadRandSaveButton();
            controller.ResetModelFilledWithDeadCells();
            mainWindow.Update();

        }
    }

    public class GameState : State
    {
        public void doAction(GameController controller, IMainWindow mainWindow)
        {
            mainWindow.SwitchToGameAreaVisual();
            mainWindow.ClearButtomsArea();
            mainWindow.AddPauseButton();

            controller.WriteLogStartOfGame();
            controller.Timer.Start();
            mainWindow.Update();
        }
    }

    public class PauseState : State
    {
        public void doAction(GameController controller, IMainWindow mainWindow)
        {
            if (controller.State is GameState)
            {
                controller.WriteLogEndOfGame();
            }

            mainWindow.SwitchToGameAreaVisual();
            mainWindow.ClearButtomsArea();
            mainWindow.AddContinueButton();
            mainWindow.AddSaveGameButton();
            mainWindow.AddLoadSaveButton();
            mainWindow.AddExitButton();


            controller.Timer.Stop();
            mainWindow.Update();
        }
    }

    public class DrawingState : State
    {
        public void doAction(GameController controller, IMainWindow mainWindow)
        {
            mainWindow.SwitchToGameAreaVisual();
            mainWindow.ClearButtomsArea();
            mainWindow.AddApplyDrawButton();
            mainWindow.AddCancelDrawButton();

            controller.GameModel.ResetModelFilledWithDeadCells();
            mainWindow.SwitchOnDrawAliveCellsState();
            mainWindow.Update();
        }
    }

    public class GameOverState : State
    {
        public void doAction(GameController controller, IMainWindow mainWindow)
        {
            if (controller.State is GameState)
            {
                controller.WriteLogEndOfGame();
            }

            mainWindow.SwitchToGameAreaVisual();
            mainWindow.ClearButtomsArea();
            mainWindow.AddExitButton();


            controller.Timer.Stop();
            MessageBox.Show("Game is over");
            mainWindow.Update();
        }
    }

    public class NewGameState : State
    {
        public void doAction(GameController controller, IMainWindow mainWindow)
        { 
            mainWindow.SwitchToGameAreaVisual();
            mainWindow.ClearButtomsArea();
            mainWindow.AddUseToroidToggle();
            mainWindow.AddAutoGenerateAliveCellsButton();
            mainWindow.AddStartButton();
            mainWindow.AddDrawAliveCellsButton();
            mainWindow.Update();
        }
    }

    public class SaveScreenState : State
    {
        public void doAction(GameController controller, IMainWindow mainWindow)
        {
            mainWindow.ClearButtomsArea();
            mainWindow.AddExitButton(GameController.FetchGameController.State);
            mainWindow.SwitchToSaveScreen();

            mainWindow.Update();
        }
    }

    public class LoadScreenState : State
    {
        public void doAction(GameController controller, IMainWindow mainWindow)
        {
            mainWindow.ClearButtomsArea();
            mainWindow.AddExitButton(GameController.FetchGameController.State);
            mainWindow.SwitchToLoadScreen();
            
            
            mainWindow.Update();
        }
    }




    public class NewGameCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            GameController.FetchGameController.State = new NewGameState();
        }

        public event EventHandler CanExecuteChanged;
    }

    public class DrawAliveCellsCommand : ICommand {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            GameController.FetchGameController.State = new DrawingState();
        }

        public event EventHandler CanExecuteChanged;
    }

    public class AutoGenAliveCellsCommand : ICommand {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            GameController.FetchGameController.RandAutoPopulateCells();
        }

        public event EventHandler CanExecuteChanged;
    }

    public class ExitCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is State)
            {
                GameController.FetchGameController.State = (State)parameter;
            }
            else
            {
                GameController.FetchGameController.State = new StartState();
            }
 
        }

        public event EventHandler CanExecuteChanged;
    }

    public class StartCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            GameController.FetchGameController.State = new GameState();
        }

        public event EventHandler CanExecuteChanged;
    }

    public class GameCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            GameController.FetchGameController.State = new GameState();
        }

        public event EventHandler CanExecuteChanged;
    }

    public class PauseCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            GameController.FetchGameController.State = new PauseState();           
        }

        public event EventHandler CanExecuteChanged;
    }

    public class SaveCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            GameController.FetchGameController.State = new SaveScreenState();
        }

        public event EventHandler CanExecuteChanged;
    }

    public class LoadCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            GameController.FetchGameController.State = new LoadScreenState();
        }

        public event EventHandler CanExecuteChanged;
    }

    public class LoadRandomCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {

            if (GameController.FetchGameController.LoadRandSave())
            {
                GameController.FetchGameController.State = new PauseState();
            }
            else
            {
                GameController.FetchGameController.State =new StartState();
            }
        }

        public event EventHandler CanExecuteChanged;
    }

    public class ApplyDrawCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            throw new NotImplementedException();
        }

        public event EventHandler CanExecuteChanged;
    }

    public interface IMainWindow
    {
        void Update();
        void  SwitchToGameAreaVisual();
        void SwitchOnDrawAliveCellsState();
        void SwitchOffDrawAliveCellsState();
        void SwitchToLoadScreen();
        void SwitchToSaveScreen();


        #region control of buttons
        void ClearButtomsArea();
        void AddStartButton();
        void AddLoadRandSaveButton();
        void AddLoadSaveButton();
        void AddNewButton();
        void AddPauseButton();
        void AddSaveGameButton();
        void AddExitButton(State returnState = null);
        void AddContinueButton();
        void AddApplyDrawButton();
        void AddCancelDrawButton();
        void AddAutoGenerateAliveCellsButton();
        void AddDrawAliveCellsButton();
        void AddUseToroidToggle();


        #endregion

    }


    #endregion




}
