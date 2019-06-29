using System.Collections.Generic;

namespace LibRobotAuto.Common
{
    public class Shelf
    {
        public int ShelfGroupID;
        public Dictionary<int, List<Book>> Books;

        public Shelf(int ID)
        {
            this.ShelfGroupID = ID;
            this.Books = new Dictionary<int, List<Book>>();
        }

        public void Add(int layer, Book book)
        {
            if (!Books.ContainsKey(layer))
            {
                Books[layer] = new List<Book>();
            }

            if (book.Type == 2)
            {
                Books[layer].Add(book);
            }
            else
            {
                int i = 0;
                for (; i < Books[layer].Count; i++)
                {
                    if (book.ScanTimeStamp < Books[layer][i].ScanTimeStamp || Books[layer][i].Type == 2)
                    {
                        Books[layer].Insert(i, book);
                        break;
                    }
                }

                if (i == Books[layer].Count)
                    Books[layer].Add(book);
            }
        }

        public void Remove(int layer, string barcode)
        {
            if (!Books.ContainsKey(layer))
                return;

            int index = -1;
            for (int i = 0; i < Books[layer].Count; i++)
            {
                if (Books[layer][i].barcode.Equals(barcode))
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                Books[layer].RemoveAt(index);
            }
        }
    }
}
