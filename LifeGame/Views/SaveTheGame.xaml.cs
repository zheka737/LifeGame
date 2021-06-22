using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
using System.Windows.Shapes;
using LifeGame.Controllers;
using LifeGame.Models;

namespace LifeGame.Views
{
    /// <summary>
    /// Interaction logic for SaveTheGame.xaml
    /// </summary>
    public partial class SaveTheGame : Page, IPageOfMainWindow
    {
        public Guid SelectedSaveGuid { get; set; }
        public string NameOfGame { get; set; } = String.Empty;

        public SaveTheGame()
        {
            InitializeComponent();
            Update();
        }

        public void Update()
        {
            ListOfSaves.Items.Clear();

            using (SqlConnection cn = WorkWithSaves.DbConnectionFactory())
            {
                var command = cn.CreateCommand();

                command.CommandText = $@"Select
	                                        Saves.Id AS IdOfSave,
	                                        Saves.[Name] AS NameOfSave,
	                                        Saves.DateOfCreation AS DateOfCreation
                                        From
	                                        [dbo].[Saves] AS Saves
                                        Order By
	                                        DateOfCreation DESC";
                command.CommandType = CommandType.Text;
                command.Connection = cn;

                bool startOfReading = true;

                using (var dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        ListOfSaves.Items.Add(new SavePresentation()
                        {
                            NameOfSave = (string)dr["NameOfSave"],
                            GuidOfSave = (Guid)dr["IdOfSave"],
                            DateOfCreation = (DateTime)dr["DateOfCreation"]
                        });

                        //var val1 = (string)dr["FieldName"];
                    }
                }
            }
        }

        private void SaveButtom_Click(object sender, RoutedEventArgs e)
        {
            if (NameOfSaveBox.Text == String.Empty && ListOfSaves.SelectedItem == null)
            {
                MessageBox.Show("Напишите имя сохранения или укажите сохранение для перезаписи");
            }

            if (ListOfSaves.SelectedItem != null)
            {
                SavePresentation result = (SavePresentation)ListOfSaves.SelectedItem;
                if (result != null)
                {
                    SelectedSaveGuid = result.GuidOfSave;
                    NameOfGame = result.NameOfSave;
                }
            }
            else if (NameOfSaveBox.Text != String.Empty)
            {
                SelectedSaveGuid = Guid.Empty;
                NameOfGame = NameOfSaveBox.Text;
            }

            if (SelectedSaveGuid != Guid.Empty || !String.IsNullOrEmpty(NameOfGame))
            {
                GameController.FetchGameController.SaveGame(SelectedSaveGuid, NameOfGame);
            }

        }



        private void DeleteButtom_Click(object sender, RoutedEventArgs e)
        {
            Guid idOfSaveToDelete;

            if (ListOfSaves.SelectedItems.Count > 0)
            {
                SavePresentation result = (SavePresentation)ListOfSaves.SelectedItems[0];


                if (result != null)
                {
                    idOfSaveToDelete = result.GuidOfSave;
                    GameController.DeleteSave(idOfSaveToDelete);
                }

            }

            Update();
        }

        private void NameOfSaveBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ListOfSaves.SelectedItem = null;
        }
    }
}
