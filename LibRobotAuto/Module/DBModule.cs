using LibRobotAuto.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace LibRobotAuto.Module
{
    public class DBModule
    {
        private static SqlConnection Conn = new SqlConnection();

        public static bool Connect()
        {
            if (Conn.State == ConnectionState.Open)
                return true;

            Conn.ConnectionString = "server=" + UserConfig.Server + ";database=" + UserConfig.Database + ";uid=" + UserConfig.Uid + ";pwd=" + UserConfig.Pwd;

            try
            {
                Conn.Open();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static int Disconnect()
        {
            Conn.Close();
            return 0;
        }

        public static Book QueryBookBy(string barcode)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "SELECT Barcode,Title,IsLoaned,ShelfID,Layer,SerialNum,GroupNum,RowNum " +
                "FROM BookCopy,Book,Shelf " +
                "WHERE BookCopy.BookID = Book.ID AND BookCopy.ShelfID = Shelf.ID AND BookCopy.Barcode = '" + barcode + "'";

            cmd.CommandType = CommandType.Text;

            SqlDataReader dataReader = cmd.ExecuteReader();
            dataReader.Read();
            Book temp_book = CreateBook(dataReader);
            dataReader.Close();

            return temp_book;
        }

        public static List<Book> QueryBooksBy(List<string> barcodes)
        {
            List<Book> books = new List<Book>();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "SELECT Barcode,Title,IsLoaned,ShelfID,Layer,SerialNum,GroupNum,RowNum " + 
                "FROM BookCopy,Book,Shelf " +
                "WHERE BookCopy.BookID = Book.ID AND BookCopy.ShelfID = Shelf.ID AND BookCopy.Barcode in (";

            for (int i = 0; i < barcodes.Count; i++)
            {
                cmd.CommandText += "'" + barcodes[i] + "'";

                if (i == barcodes.Count - 1)
                    cmd.CommandText += ")";
                else
                    cmd.CommandText += ",";
            }

            cmd.CommandType = CommandType.Text;

            SqlDataReader dataReader = cmd.ExecuteReader();
            while (dataReader.Read())
                books.Add(CreateBook(dataReader));

            dataReader.Close();

            return books;
        }

        public static List<Book> QueryBooksBy(int group, int row)
        {
            List<Book> books = new List<Book>();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = Conn;

            cmd.CommandText = "SELECT Barcode,Title,IsLoaned,ShelfID,Layer,SerialNum,GroupNum,RowNum " +
                "FROM BookCopy,Book,Shelf " +
                "WHERE BookCopy.BookID = Book.ID AND BookCopy.ShelfID = Shelf.ID AND BookCopy.IsLoaned = 1 AND Shelf.GroupNum = " + group + " AND Shelf.RowNum = " + row;

            cmd.CommandType = CommandType.Text;

            SqlDataReader dataReader = cmd.ExecuteReader();
            while (dataReader.Read())
                books.Add(CreateBook(dataReader));

            dataReader.Close();

            return books;
        }

        public static int GetShelfID(int group, int row)
        {
            int ShelfID;
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = Conn;

            cmd.CommandText = "SELECT ID FROM Shelf WHERE Shelf.GroupNum = " + group + " AND Shelf.RowNum = " + row;

            cmd.CommandType = CommandType.Text;
            SqlDataReader dataReader = cmd.ExecuteReader();

            dataReader.Read();
            ShelfID = Int32.Parse(dataReader[0].ToString());
            dataReader.Close();

            return ShelfID;
        }

        public static int UpdateBookInfo(Book book)
        {
            int ShelfID = GetShelfID(book.ScanGroup, book.ScanRow);
            int Layer = book.ScanLayer - 1;

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = Conn;
            cmd.CommandText = "UPDATE BookCopy SET ShelfID = " + ShelfID + ", Layer = " + Layer + " WHERE Barcode = '" + book.barcode + "'";

            return cmd.ExecuteNonQuery();
        }

        private static Book CreateBook(SqlDataReader dataReader)
        {
            return new Book(dataReader[0].ToString(),
                    dataReader[1].ToString(),
                    (bool)dataReader[2],
                    Int32.Parse(dataReader[3].ToString()),
                    (int)dataReader[4] + 1,
                    (int)dataReader[5],
                    (int)dataReader[6],
                    (int)dataReader[7]);
        }
    }
}
