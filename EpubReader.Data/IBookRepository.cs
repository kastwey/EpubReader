using EpubReader.Data.Entities;
using System.Collections.Generic;

namespace EpubReader.Data
{
	public interface IBookRepository
	{
		void Add(Book book);
		void Delete(Book book);
		void Update(Book book);
		List<Book> GetAll();

		void Clear();

	}
}