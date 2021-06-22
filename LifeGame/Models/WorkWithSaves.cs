using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LifeGame.Model;

namespace LifeGame.Models
{
    public static class WorkWithSaves
    {
        private static object lockObj = new object();

        private static string nameOfDbForLifeGame = "LifeGame12345678";
        public static void SaveGame(Guid idOfSave, string nameOfSave, List<Cell> cells, int areaSizeX, int areaSizeY, int cellSize, bool useToroid)
        {
            if (idOfSave == Guid.Empty)
            {
                idOfSave = Guid.NewGuid();
            }

            string nameOfSameSave = String.Empty;

            if (!CheckThatSaveIsUnique(cells, areaSizeX, areaSizeY, cellSize, useToroid, ref nameOfSameSave))
            {
                MessageBox.Show("Не удалось сохранить, т.к. уже существует точно такое же сохранение под названием " +
                                nameOfSameSave);
                return;
            }

            using (SqlConnection cn = DbConnectionFactory())
            {
                SqlTransaction tx = null;

                try
                {
                    tx = cn.BeginTransaction();

                    DeleteSave(idOfSave, cn, tx);

                    AddSaveEntry(idOfSave, nameOfSave, areaSizeX, areaSizeY, cellSize, useToroid, cn, tx);
                    SaveCellsPositions(idOfSave, cells, cn, tx);

                    tx.Commit();
                }
                catch (Exception e)
                {
                    tx?.Rollback();
                }

            }
        }

        public static void AddSaveEntry(Guid idOfSave, string nameOfSave,  int areaSizeX, int areaSizeY, int cellSize, bool useToroid, SqlConnection cn, SqlTransaction tx = null)
        {
            int useToroidConverter = useToroid ? 1 : 0;

            SqlCommand command = new SqlCommand($"INSERT INTO [dbo].[Saves] ([Id], [Name], [GameAreaSizeX], [GameAreaSizeY], [CellSize], [UseToroid]) " +
                                                $"VALUES ('{idOfSave}','{nameOfSave}', {areaSizeX}, {areaSizeY}, {cellSize}, {useToroidConverter})", cn);
            command.Transaction = tx;
            command.ExecuteNonQuery();
        }

        //TODO Optimise to only save alive cells
        public static void SaveCellsPositions(Guid idOfSave, List<Cell> cells, SqlConnection cn, SqlTransaction tx = null)
        {
            string query = String.Empty;
            

            foreach (Cell cell in cells)
            {
                int boolConvertor = cell.Alive ? 1 : 0;

                query += $"\n INSERT INTO [dbo].[SavedCells] ([Id], [Save], [X], [Y], [Alive]) " +
                    $"VALUES ('{Guid.NewGuid()}','{idOfSave}', {cell.PositionX}, {cell.PositionY}" +
                    $", {boolConvertor});";

            }
            SqlCommand command = new SqlCommand(query, cn);
            command.Transaction = tx;
            command.ExecuteNonQuery();
        }

        public static void DeleteSave(Guid idOfSave, SqlConnection cn, SqlTransaction tx)
        {
            SqlCommand command = new SqlCommand($"DELETE FROM [dbo].[Saves] WHERE [Id] = '{idOfSave}'", cn);
            command.Transaction = tx;
            command.ExecuteNonQuery();
        }

        public static SqlConnection DbConnectionFactory()
        {
            ConnectionStringSettings connectionDetails =
                ConfigurationManager.ConnectionStrings["DefaultConnection"];

            var providerName = connectionDetails.ProviderName;
            var connectionString = connectionDetails.ConnectionString;

            SqlConnection cn = null;

            try
            {
                cn = new SqlConnection(connectionString);
                cn.Open();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Проверьте строку подключения к sql серверу");
                throw;
            }

            if (CheckDatabaseExists(cn, nameOfDbForLifeGame))
            {
                cn.ChangeDatabase(nameOfDbForLifeGame);
            }
            else
            {
                CreateNewDatabaseForLifeGame(nameOfDbForLifeGame, cn);
                cn.ChangeDatabase(nameOfDbForLifeGame);
                CreateSchemaForNewDatabase(cn);
            }

            return cn;
        }

        private static void CreateNewDatabaseForLifeGame(string nameOfDbForLifeGame, SqlConnection cn)
        {
            string CreateDatabase = "CREATE DATABASE " + nameOfDbForLifeGame + " ; ";
            SqlCommand command = new SqlCommand(CreateDatabase, cn);

            command.ExecuteNonQuery();
           
        }

        public static bool CheckDatabaseExists(SqlConnection cn, string nameOfDatabase)
        {
            string sqlCreateDBQuery;
            bool result = false;

            try
            {
                sqlCreateDBQuery = string.Format("SELECT database_id FROM sys.databases WHERE Name = '{0}'", nameOfDatabase);
                using (SqlCommand sqlCmd = new SqlCommand(sqlCreateDBQuery, cn))
                {
                    object resultObj = sqlCmd.ExecuteScalar();
                    int databaseID = 0;
                    if (resultObj != null)
                    {
                        int.TryParse(resultObj.ToString(), out databaseID);
                    }
                    result = (databaseID > 0);
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        public static void CreateSchemaForNewDatabase(SqlConnection cn)
        {
            string logTableCreationCommand = @"CREATE TABLE [dbo].[Log]
                                             (
	                                            [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
                                                [Start] DATETIME NULL, 
                                                [End] DATETIME NULL
                                             )";

            string savesTableCreationCommand = @"CREATE TABLE [dbo].[Saves]
                                                (
	                                                [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
                                                    [Name] NVARCHAR(50) NOT NULL DEFAULT 'no name', 
                                                    [DateOfCreation] DATETIME NOT NULL DEFAULT GETDATE(),
                                                    [GameAreaSizeX] INT NOT NULL, 
                                                    [GameAreaSizeY] INT NOT NULL, 
                                                    [CellSize] INT NOT NULL, 
                                                    [UseToroid] BIT NOT NULL 
                                                )";
            string savedCellsTableCreationCommand = @"CREATE TABLE [dbo].[SavedCells]
                                                    (
	                                                    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
                                                        [Save] UNIQUEIDENTIFIER NOT NULL, 
                                                        [X] INT NOT NULL, 
                                                        [Y] INT NOT NULL, 
                                                        [Alive] BIT NOT NULL, 
                                                        CONSTRAINT [FK_SavedCells_ToTable] FOREIGN KEY ([Save]) REFERENCES [Saves]([Id]) ON DELETE CASCADE
                                                    )
                                                    ";
            SqlCommand command = new SqlCommand(logTableCreationCommand, cn);

            command.ExecuteNonQuery();

            command = new SqlCommand(savesTableCreationCommand, cn);

            command.ExecuteNonQuery();

            command = new SqlCommand(savedCellsTableCreationCommand, cn);

            command.ExecuteNonQuery();

        }

        public static void DropLifeGameDB()
        {
            using (SqlConnection cn = DbConnectionFactory())
            {
                cn.ChangeDatabase("IdentityUsers");
                SqlCommand command = new SqlCommand("DROP DATABASE " + nameOfDbForLifeGame + " ; ", cn);
                command.ExecuteNonQuery();
            }
        }


        public static SaveOfLiveGame FetchSaveOfLiveGame(Guid idOfSave)
        {
            SaveOfLiveGame result =  new SaveOfLiveGame();

            using (SqlConnection cn = DbConnectionFactory())
            {
                var command = cn.CreateCommand();

                command.CommandText = $@"Select
	                                        SavedCells.X AS X,
	                                        SavedCells.Y AS Y,
	                                        SavedCells.Alive AS Alive,
	                                        Saves.[Name] AS NameOfSafe,
	                                        Saves.DateOfCreation AS DateOfCreation,
	                                        Saves.GameAreaSizeX AS GameAreaSizeX,
	                                        Saves.GameAreaSizeY AS GameAreaSizeY,
	                                        Saves.CellSize AS CellSize,
                                            Saves.UseToroid AS UseToroid
                                        From
	                                        [dbo].[SavedCells] AS SavedCells
	                                        Left Join
	                                        [dbo].[Saves] As Saves ON SavedCells.[Save] = Saves.[Id]
                                        Where
                                            SavedCells.[Save] = '{idOfSave}';";
                command.CommandType = CommandType.Text;
                command.Connection = cn;

                bool startOfReading = true;

                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        if (startOfReading)
                        {
                            result.Id = idOfSave;
                            result.GameAreaSizeX = (int)dr["GameAreaSizeX"];
                            result.GameAreaSizeY = (int)dr["GameAreaSizeY"];
                            result.CellSize = (int)dr["CellSize"];
                            result.NameOfSave = (string)dr["NameOfSafe"];
                            result.UseToroid = (bool) dr["UseToroid"];
                            startOfReading = false;
                        }
                        result.Cells.Add(new Cell((int)dr["X"], (int)dr["Y"])
                        {
                            Alive = (bool)dr["Alive"]
                        });


                        //var val1 = (string)dr["FieldName"];
                    }
                }
            }



            return result;
        }

        public static bool CheckThatSaveIsUnique(List<Cell> cells, int areaSizeX, int areaSizeY, int cellSize, bool useToroid, ref string nameOfSameSave)
        {
            int boolConverter = useToroid ? 1 : 0;
            List<Guid> result = new List<Guid>();
            List<Cell> aliveCells = cells.Where(cell => cell.Alive).ToList();

            if (aliveCells.Count < 3)
            {
                return false;
            }

            using (SqlConnection cn = DbConnectionFactory())
            {
                var command = cn.CreateCommand();

                command.CommandText = $@"Select
	                                        [Saves].Id As IdOfSave
                                        From
	                                        (Select * 
	                                        From [dbo].Saves As S
	                                        Where S.GameAreaSizeX = {areaSizeX} And S.GameAreaSizeY = {areaSizeY} And S.CellSize = {cellSize} And S.UseToroid = {boolConverter}) As Saves
	                                        Cross Apply
	                                        (Select
		                                        Count(SavedCells.Id) AS NumberOfMatches
	                                         From
		                                        [dbo].SavedCells As SavedCells
	                                          Where
		                                         SavedCells.[Save] = [Saves].Id And SavedCells.Alive = 1 And ((SavedCells.X = {aliveCells[0].PositionX} And SavedCells.Y = {aliveCells[0].PositionY}) 
			                                        Or (SavedCells.X = {aliveCells[1].PositionX} And SavedCells.Y = {aliveCells[1].PositionY})  
                                                        Or (SavedCells.X = {aliveCells[2].PositionX} And SavedCells.Y = {aliveCells[2].PositionY})) ) As SavedCells
                                        Where SavedCells.NumberOfMatches = 3";
                command.CommandType = CommandType.Text;
                command.Connection = cn;

                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        result.Add((Guid)dr["IdOfSave"]);
                        //var val1 = (string)dr["FieldName"];
                    }
                }
            }

            if (result.Count == 0)
            {
                return true;
            }
            else
            {
                bool isUnique = true;
                string nameOfSameSaveTemp = String.Empty;
                

                Parallel.ForEach(result, guidOfSave =>
                {
                    if (!isUnique) { return; }

                    SaveOfLiveGame saveObject = FetchSaveOfLiveGame(guidOfSave);

                    if (Cell.CompareListsOfCells(cells, saveObject.Cells, new CellsComparerByPositionAndState()))
                    {
                        lock (lockObj)
                        {
                            isUnique = false;
                            nameOfSameSaveTemp = saveObject.NameOfSave;
                        }
                    }

                });

                nameOfSameSave = nameOfSameSaveTemp;
                return isUnique;
            }
        }

    }


    public class SaveOfLiveGame
    {
        public Guid Id { get; set; }
        public string NameOfSave { get; set; }
        public List<Cell> Cells { get; set; }
        public int GameAreaSizeX { get; set; }
        public int GameAreaSizeY { get; set; }

        public int CellSize { get; set; }
        public bool UseToroid { get; set; }

        public SaveOfLiveGame()
        {
            Cells = new List<Cell>();
        }

    }
}
