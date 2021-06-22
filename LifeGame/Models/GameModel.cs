using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LifeGame.Model;

namespace LifeGame.Models
{
    public class GameModel
    {
        Object lockObj = new object();
        public int GameModelAreaSizeX { get; }
        public int GameModelAreaSizeY { get; }
        public int CellSize { get; }

        public int NumberOfCellsThatFitByHeight
        {
            get
            {
                if (CellSize == 0)
                {
                    throw new Exception();
                }

               return GameModelAreaSizeY / CellSize;
            }
        }

        public int NumberOfCellsThatFitByWidth
        {
            get
            {
                if (CellSize == 0)
                {
                    throw new Exception();
                }

                return GameModelAreaSizeX / CellSize;
            }
        }

        public bool UseToroid { get; set; }

        public List<Cell> Cells { get; set; }

        private Random random = new Random();
        CellsComparerByPosition cellsComparerByPosition = new CellsComparerByPosition();
        public event EventHandler GameIsOver;

        public GameModel(int gameModelAreaSizeX, int gameModelAreaSizeY, int cellSize, bool useToroid)
        {
            GameModelAreaSizeY = gameModelAreaSizeY;
            GameModelAreaSizeX = gameModelAreaSizeX;
            CellSize = cellSize;
            UseToroid = useToroid;
            Cells = new List<Cell>();

            
            //RandAutoPopulateCells();
            ResetModelFilledWithDeadCells();

        }

        public void ResetModelFilledWithDeadCells()
        {
            Cells.Clear();
            PopulateRemainedSpaceWithDeadCells();
        }

        public void RandAutoPopulateCells()
        {
            Cells.Clear();

            int maxNumberOfAliveCells = NumberOfCellsThatFitByHeight* NumberOfCellsThatFitByWidth;
            int randNumberAliveOfCells = random.Next(1, maxNumberOfAliveCells);

            for (int i = 0; i < randNumberAliveOfCells; i++)
            {
                AddCellToGameModelWithCheckOfOverlapping(FetchGenerateRandomAliveCell());
            }

            PopulateRemainedSpaceWithDeadCells();
        }

        public void NextGeneration()
        {
            List<Cell> CellsThatBecomeAlive = new List<Cell>();
            List<Cell> CellsThatBecomeDead = new List<Cell>();

            Parallel.ForEach(Cells, cell =>
            {
                int numberOfAliveCells = FetchNumberOfAliveNeighboursOfCell(cell);

                if (!cell.Alive && numberOfAliveCells == 3)
                {
                    lock (lockObj) { CellsThatBecomeAlive.Add(cell); }
                   
                }
                else if (cell.Alive && (numberOfAliveCells < 2 ||
                                        numberOfAliveCells > 3))
                {

                    lock (lockObj) { CellsThatBecomeDead.Add(cell); }
                    
                }
            });

            if (CheckForSelfLoop(CellsThatBecomeAlive, CellsThatBecomeDead))
            {
                if (GameIsOver != null) GameIsOver(null, null);
            }



            foreach (Cell cell in Cells)
            {
                cell.PreviousState = cell.Alive;
            }


            foreach (Cell cell in CellsThatBecomeAlive)
            {
                cell.Alive = true;
            }

            foreach (Cell cell in CellsThatBecomeDead)
            {
                cell.Alive = false;
            }           
        }

        public bool CheckForSelfLoop(List<Cell> CellsThatBecomeAlive, List<Cell> CellsThatBecomeDead)
        {
            bool modelIsStall = true;

            Parallel.ForEach(Cells, cell =>
            {
                if (!modelIsStall)
                {
                    return;
                }

                if (cell.PreviousState == null || ((!cellIsStall(cell, CellsThatBecomeAlive, CellsThatBecomeDead)) &&
                                                   (!cellIsLooped(cell, CellsThatBecomeAlive, CellsThatBecomeDead))))
                {
                    lock (lockObj)
                    {
                        modelIsStall = false;
                    }

                }
            });


            //foreach (Cell cell in Cells)
            //{
            //    if (cell.PreviousState == null || ((!cellIsStall(cell, CellsThatBecomeAlive, CellsThatBecomeDead)) &&
            //        (!cellIsLooped(cell, CellsThatBecomeAlive, CellsThatBecomeDead))))
            //    {
            //        modelIsStall = false;
            //    }
            //}

            return modelIsStall;
        }

        bool cellIsStall(Cell cellToExamine, List<Cell> CellsThatBecomeAlive, List<Cell> CellsThatBecomeDead)
        {
            return cellToExamine.PreviousState == cellToExamine.Alive == futureStateOfModelIsAlive(cellToExamine, CellsThatBecomeAlive, CellsThatBecomeDead);
        }

        bool cellIsLooped(Cell cellToExamine, List<Cell> CellsThatBecomeAlive, List<Cell> CellsThatBecomeDead)
        {
            return cellToExamine.PreviousState == futureStateOfModelIsAlive(cellToExamine, CellsThatBecomeAlive, CellsThatBecomeDead);
        }

        bool futureStateOfModelIsAlive(Cell cellToExamine, List<Cell> CellsThatBecomeAlive, List<Cell> CellsThatBecomeDead)
        {
            if (cellToExamine.Alive && CellsThatBecomeDead.Contains(cellToExamine, cellsComparerByPosition))
            {
                return false;
            }
            else if ((!cellToExamine.Alive) && CellsThatBecomeAlive.Contains(cellToExamine, cellsComparerByPosition))
            {
                return true;
            }

            return cellToExamine.Alive;
        }



        public int FetchNumberOfAliveNeighboursOfCell(Cell cell)
        {
            //TODO Optimise, Max 4 alive cells is important
            Cell upCell = FindCellByCoordinatesInCells(cell.PositionX, cell.PositionY - CellSize);
            Cell rigntCell = FindCellByCoordinatesInCells(cell.PositionX + CellSize, cell.PositionY);
            Cell downCell = FindCellByCoordinatesInCells(cell.PositionX, cell.PositionY + CellSize);
            Cell leftCell = FindCellByCoordinatesInCells(cell.PositionX - CellSize, cell.PositionY);

            Cell upLeftCell = FindCellByCoordinatesInCells(cell.PositionX - CellSize, cell.PositionY - CellSize);
            Cell upRightCell = FindCellByCoordinatesInCells(cell.PositionX + CellSize, cell.PositionY - CellSize);
            Cell downRightCell = FindCellByCoordinatesInCells(cell.PositionX + CellSize, cell.PositionY + CellSize);
            Cell downLeftCell = FindCellByCoordinatesInCells(cell.PositionX - CellSize, cell.PositionY + CellSize);

            List<Cell> NeightbourCells = new List<Cell>()
            {
                upCell, rigntCell, downCell, leftCell, upLeftCell, upRightCell, downRightCell, downLeftCell
            };

            if (UseToroid && NeightbourCells.Contains(null))
            {
                throw new Exception("Cannot find cell by specified coordinates");
            }

            var result = (NeightbourCells.Where(cell1 =>
            {
                return cell1 != null && cell1.Alive;
            })).Count();

            return result;
        }

        public Cell FindCellByCoordinatesInCells(int coordinateX, int coordinateY)
        {
            //TODO Are down-right corner connected with up-left??

            if (UseToroid)
            {
                if (coordinateX < 0)
                {
                    coordinateX = (NumberOfCellsThatFitByWidth - 1) * CellSize;
                }

                if (coordinateY < 0)
                {
                    coordinateY = (NumberOfCellsThatFitByHeight - 1) * CellSize;
                }

                if (coordinateX >= NumberOfCellsThatFitByWidth * CellSize)
                {
                    coordinateX = 0;
                }

                if (coordinateY >= NumberOfCellsThatFitByHeight * CellSize)
                {
                    coordinateY = 0;
                }
            }


            Cell result = Cells.Find(cell =>
            {
                return cell.PositionX == coordinateX && cell.PositionY == coordinateY;
            });

            return result;
        }

        //Fill empty space with dead cells
        public void PopulateRemainedSpaceWithDeadCells()
        {


            for (int i = 0; i < NumberOfCellsThatFitByWidth; i++)
            {
                for (int j = 0; j < NumberOfCellsThatFitByHeight; j++)
                {
                    int positionXForCheck = i * CellSize;
                    int positionYForCheck = j * CellSize;

                    AddCellToGameModelWithCheckOfOverlapping(new Cell(positionXForCheck, positionYForCheck)
                    {
                        Alive = false
                    });
                }
            }
        }


        public Cell FetchGenerateRandomAliveCell()
        {
            return  new Cell(random.Next(0, NumberOfCellsThatFitByWidth)* CellSize
                , random.Next(0, NumberOfCellsThatFitByHeight)* CellSize)
            {
                Alive = true
            };
        }

        public void AddCellToGameModelWithCheckOfOverlapping(Cell cell)
        {
            if (!Cells.Contains(cell, cellsComparerByPosition))
            {
                Cells.Add(cell);
            }
        }

        public void SaveGame(Guid idOfSave, string nameOfSave)
        {
            WorkWithSaves.SaveGame(idOfSave, nameOfSave, Cells, GameModelAreaSizeX, GameModelAreaSizeY, CellSize, UseToroid);
        }

        public void LoadGame(Guid idOfSave)
        {
            Cells.Clear();
            SaveOfLiveGame save = WorkWithSaves.FetchSaveOfLiveGame(idOfSave);

            if (save.GameAreaSizeX != GameModelAreaSizeX || save.GameAreaSizeY != GameModelAreaSizeY
                                                         || save.CellSize != CellSize)
            {
                throw new Exception("Не удается загрузить сохранение, т.к. у него другой размер поля");
            }

            Cells = save.Cells;
            UseToroid = save.UseToroid;
        }

        /// <summary>
        /// returns true is success
        /// </summary>
        /// <returns></returns>
        public bool LoadRandomSave()
        {
            List<Guid> resultIds = new List<Guid>();
            Random rand = new Random();

            using (SqlConnection cn = WorkWithSaves.DbConnectionFactory())
            {
                var command = cn.CreateCommand();

                command.CommandText = $@"Select
	                                        [Saves].[Id] AS Id
                                        From
	                                        [dbo].[Saves] As Saves
                                        Where
	                                        [Saves].GameAreaSizeX = {GameModelAreaSizeX} AND
	                                        [Saves].GameAreaSizeY = {GameModelAreaSizeY} AND
	                                        [Saves].CellSize = {CellSize} ;";
                command.CommandType = CommandType.Text;
                command.Connection = cn;

                bool startOfReading = true;

                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        resultIds.Add((Guid)dr["Id"]);
                    }
                }
            }

            if (resultIds.Count == 0)
            {
                MessageBox.Show("Нет ни одного сохранения для загрузки");
                return false;
            }
            else
            {
                LoadGame(resultIds[rand.Next(resultIds.Count)]);
                return true;
            }


        }

  

        public Guid WriteLogStartOfGame()
        {
            Guid guidOfStartEntry = Guid.NewGuid();

            using (SqlConnection cn = WorkWithSaves.DbConnectionFactory())
            {
                SqlCommand command = new SqlCommand($"INSERT INTO [dbo].[Log] ([Id], [Start], [End]) " +
                                                    $" VALUES ('{guidOfStartEntry}','{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}', '')", cn);
                command.ExecuteNonQuery();
            }

            return guidOfStartEntry;
        }

        public void WriteLogEndOfGame(Guid guidOfStartEntry)
        {
            using (SqlConnection cn = WorkWithSaves.DbConnectionFactory())
            {
                SqlCommand command = new SqlCommand($"UPDATE [dbo].[Log] Set [End] = '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}' WHERE [dbo].[Log].[Id] = '{guidOfStartEntry}'", cn);
                command.ExecuteNonQuery();
            }
        }

        public List<string> RetrieveAllLog()
        {
            List<string> result = new List<string>();

            using (SqlConnection cn = WorkWithSaves.DbConnectionFactory())
            {
                var command = cn.CreateCommand();

                command.CommandText = $@"Select
	                                        [Log].[Start] AS [Start],
	                                        [Log].[End] AS [End]
                                        From
	                                        [dbo].[Log] AS [Log]
                                        Order By
	                                        [Start] DESC;";
                command.CommandType = CommandType.Text;
                command.Connection = cn;

                bool startOfReading = true;

                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        DateTime starTime = (DateTime) dr["Start"];
                        DateTime endTime = (DateTime)dr["End"];
                        if (endTime < DateTime.Parse("01/11/2000 00:00:00"))
                        {
                            result.Add($"Игра началась в {starTime}");
                        }
                        else
                        {
                            result.Add($"Игра началась в {starTime} и закончилась в {endTime}");
                        }
                        //var val1 = (string)dr["FieldName"];
                    }
                }

            }

            return result;
        }

        
    }
}
