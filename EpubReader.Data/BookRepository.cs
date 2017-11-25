using EpubReader.Data.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace EpubReader.Data
{
	public class BookRepository : IBookRepository
	{
		private readonly List<Book> _books;
		private readonly string _jsonPath;

		public BookRepository()
		{
			_jsonPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "books.json");
			if (File.Exists(_jsonPath))
			{
				_books = JsonConvert.DeserializeObject<List<Book>>(File.ReadAllText(_jsonPath, Encoding.UTF8));
			}
			else
			{
				_books = new List<Book>();
			}
		}

		private void UpdateJson()
		{
			File.WriteAllText(_jsonPath, JsonConvert.SerializeObject(_books), Encoding.UTF8);
		}

		public void Add(Book book)
		{
			_books.Add(book);
			UpdateJson();
		}

		public void Delete(Book book)
		{
			_books.Remove(book);
			UpdateJson();
		}

		public void Update(Book book)
		{
			UpdateJson();
		}

		public List<Book> GetAll()
		{
			return _books;
		}

		public void Clear()
		{
			_books.Clear();
			UpdateJson();
		}
	}
}
