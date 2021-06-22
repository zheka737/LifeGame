using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeGame.Model
{
    //TODO change to struct
    public class Cell
    {
        private bool _alive = false;

        public int PositionX { get; set; }
        public int PositionY { get; set; }

        public bool Alive
        {
            get { return _alive; }
            set
            {
                _alive = value;
            }
        }

        public bool? PreviousState { get; set; } = null;

        public Cell(int positionX, int positionY)
        {
            PositionX = positionX;
            PositionY = positionY;
            Alive = false;
        }

        public static bool CompareListsOfCells(List<Cell> list1, List<Cell> list2, IEqualityComparer<Cell> comparer)
        {
            var firstNotSecond = list1.Except(list2, comparer).ToList();
            var secondNotFirst = list2.Except(list1, comparer).ToList();
            return !firstNotSecond.Any() && !secondNotFirst.Any();
        }
    }

    public class CellsComparerByPosition:IEqualityComparer<Cell>
    {
        public bool Equals(Cell x, Cell y)
        {
            return x.PositionX == y.PositionX && x.PositionY == y.PositionY;
        }

        public int GetHashCode(Cell obj)
        {
            throw new NotImplementedException();
        }
    }

    public class CellsComparerByPositionAndState : IEqualityComparer<Cell>
    {
        public bool Equals(Cell x, Cell y)
        {
            return x.PositionX == y.PositionX && x.PositionY == y.PositionY && x.Alive == y.Alive;
        }

        public int GetHashCode(Cell obj)
        {
            return 0;
        }
    }


}
