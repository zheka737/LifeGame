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
    /// Interaction logic for ChooseSaveToLoad.xaml
    /// </summary>
    public partial class ChooseSaveToLoad : Page, IPageOfMainWindow
    {
        public Guid SelectedSaveGuid { get; set; }

        public ChooseSaveToLoad()
        {
            InitializeComponent();
            Update();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (ListOfSaves.SelectedItems.Count > 0)
            {
                SavePresentation result = (SavePresentation) ListOfSaves.SelectedItems[0];


                if (result != null)                 
                    SelectedSaveGuid = result.GuidOfSave;

                if (SelectedSaveGuid != Guid.Empty)
                {
                    GameController.FetchGameController.LoadGame(SelectedSaveGuid);
                }
            }
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

        private void Delete_OnClick_Click(object sender, RoutedEventArgs e)
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
    }

 

    public class SavePresentation
    {
        public Guid GuidOfSave { get; set; }
        public string NameOfSave { get; set; }
        public DateTime DateOfCreation { get; set; }

        public override string ToString()
        {
            return NameOfSave + " " + DateOfCreation;
        }
    }
}
