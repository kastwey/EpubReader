using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EpubReader.Models;
using ElectronNET.API;
using ElectronNET.API.Entities;
using EpubReader.Data;
using EpubReader.Library;
using System.Text.RegularExpressions;
using EpubReader.Extensions;
using System.Text;
using System.Web;
using System.IO;

namespace EpubReader.Controllers
{
	public class HomeController : Controller
	{
		private static IBookRepository _bookRepository;
		private static bool _loadedBefore = false;


		public HomeController(IBookRepository repository)
		{
			if (_bookRepository == null)
			{
				_bookRepository = repository;
			}
		}

		internal static void ListenForBookMenu()
		{
			Electron.IpcMain.On("open-book-menu", async (path) =>
			{
				string strPath = path as string;
				var book = _bookRepository.GetAll().Single(b => b.Path.Equals(strPath, StringComparison.InvariantCultureIgnoreCase));
				PopupContextMenu(book);
			});
		}

		private static void ListenForAddNewBook()
		{
			Electron.IpcMain.On("select-file", async (args) =>
			{
				var mainWindow = Electron.WindowManager.BrowserWindows.First();
				var options = new OpenDialogOptions
				{
					Properties = new OpenDialogProperty[] {
						OpenDialogProperty.openFile
					},
					Filters = new FileFilter[] {
							new FileFilter { Extensions = new string[] { "*.epub" }, Name = "Archivos en formato Epub (*.epub)" }
						}
				};

				string[] files = await Electron.Dialog.ShowOpenDialogAsync(mainWindow, options);
				if (files.Any())
				{
					AddBookToLibrary(files.First());
					Electron.IpcMain.Send(mainWindow, "select-file-reply");
				}
			});
		}

		private static void CalifyBook(Data.Entities.Book book, int stars)
		{
			var mainWindow = Electron.WindowManager.BrowserWindows.First();
			book.Stars = Convert.ToUInt16(stars);
			_bookRepository.Update(book);
			Electron.IpcMain.Send(mainWindow, "reload-screen");
		}

		private static void PopupContextMenu(Data.Entities.Book book)
		{
			var mainWindow = Electron.WindowManager.BrowserWindows.First();

			var menu = new MenuItem[]
			{
				new MenuItem
				{
					Label = "Leer libro",
					Click = async () => Electron.IpcMain.Send(mainWindow, "read-book", book.Path)
				},
				// new MenuItem { Type = MenuType.separator },
				new MenuItem {
					Label = "Eliminar libro de la biblioteca",
					Type = MenuType.normal,
					Click = async () => {
						var result = await Electron.Dialog.ShowMessageBoxAsync(new MessageBoxOptions("¿Seguro que deseas eliminar el libro de tu biblioteca?") {
							Type = MessageBoxType.question,
							Buttons = new string[] { "Sí", "No" },
							Title = "Eliminar libro de tu biblioteca"
						});
						if (result.Response == 1)
						{
							_bookRepository.Delete(book);
							Electron.IpcMain.Send(mainWindow, "reload-screen");
						}
					}
				},
				new MenuItem {
					Type = MenuType.submenu, Label = "Calificar", Submenu = new MenuItem[] {
						new MenuItem { Label = "1 estrella", Type = MenuType.checkbox, Checked = book.Stars == 1, Click = () => CalifyBook(book, 1) },
						new MenuItem { Label = "2 estrellas", Type = MenuType.checkbox, Checked = book.Stars == 2 , Click = () => CalifyBook(book, 2) },
						new MenuItem { Label = "3 estrellas", Type = MenuType.checkbox, Checked = book.Stars == 3 , Click = () => CalifyBook(book, 3) },
						new MenuItem { Label = "4 estrellas", Type = MenuType.checkbox, Checked = book.Stars == 1 , Click = () => CalifyBook(book, 4) },
						new MenuItem { Label = "5 estrellas", Type = MenuType.checkbox, Checked = book.Stars == 5 , Click = () => CalifyBook(book, 5) }
					}
				}
			};

			Electron.Menu.SetContextMenu(mainWindow, menu);
			Electron.Menu.ContextMenuPopup(mainWindow);
		}



		public IActionResult Index()
		{
			if (!_loadedBefore)
			{
				_loadedBefore = true;
				if (HybridSupport.IsElectronActive)
				{
					ListenForAddNewBook();
					ListenForBookMenu();
				}
			}
			return View(_bookRepository.GetAll());
		}

		private static void AddBookToLibrary(string file)
		{
			if (!System.IO.File.Exists(file))
			{
				throw new FileNotFoundException(file);
			}
			var book = Library.EpubReader.ReadBook(file);
			_bookRepository.Add(new Data.Entities.Book { Title = book.Title, Author = book.Author, LastOpened = null, Path = file, Stars = 0 });
		}

		[HttpGet]
		public IActionResult Read(string path)
		{
			if (!System.IO.File.Exists(path))
			{
				return NotFound("The book path was not found.");
			}
			var book = EpubReader.Library.EpubReader.ReadBook(path);
			var bookFromLibrary = _bookRepository.GetAll().SingleOrDefault(b => b.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase));
			if (bookFromLibrary != null)
			{
				bookFromLibrary.LastOpened = DateTime.Now;
				_bookRepository.Update(bookFromLibrary);
			}
			var bookModel = new Models.book
			{
				Title = book.Title,
				Author = book.Author,
				FileName = path
			};
			List<Models.Chapter> chapters = new List<Models.Chapter>();
			Func<EpubChapter, Chapter> funcConvert = null;
			funcConvert = new Func<EpubChapter, Chapter>((epChapter) =>
			{
				Chapter chapter = new Chapter { FileName = epChapter.ContentFileName, Title = epChapter.Title, Book = bookModel };
				if (epChapter.SubChapters != null && epChapter.SubChapters.Any())
				{
					foreach (var epSubChapter in epChapter.SubChapters)
					{
						chapter.Chapters.Add(funcConvert(epSubChapter));
					}
				}
				return chapter;
			});
			foreach (var epChapter in book.Chapters)
			{
				chapters.Add(funcConvert(epChapter));
			}
			bookModel.Chapters = chapters;

			if (HybridSupport.IsElectronActive)
			{
				Electron.IpcMain.On("export-book", async (args) =>
				{
					var mainWindow = Electron.WindowManager.BrowserWindows.First();
					var options = new SaveDialogOptions
					{
						Filters = new FileFilter[] {
							new FileFilter { Extensions = new string[] { "*.txt" }, Name = "Archivos de texto plano(*.txt)" }
						},
						Title = "Exportar libro a texto plano"
					};

					string file = await Electron.Dialog.ShowSaveDialogAsync(mainWindow, options);
					if (!String.IsNullOrWhiteSpace(file))
					{
						try
						{
							var bookToExport = EpubReader.Library.EpubReader.ReadBook(path);
							var regex = new Regex(@"<[^>]*>", RegexOptions.IgnoreCase);
							List<EpubChapter> allChapters = bookToExport.GetAllChapters();
							string wholeBook = String.Join(Environment.NewLine, allChapters.Select(ch =>
							{
								return regex.Replace(ch.HtmlContent, String.Empty);
							}).ToArray());
							System.IO.File.WriteAllText(file, wholeBook, Encoding.UTF8);

							Electron.IpcMain.Send(mainWindow, "export-book-reply", true);
						}
						catch (Exception ex)
						{
							Electron.IpcMain.Send(mainWindow, "export-file-reply", ex.Message);
						}
					}
				});
			}

			return View(bookModel);
		}

		[Produces("text/html")]
		[Route("Chapter/{path}/{chapter}")]
		public string ReadChapter(string path, string chapter)
		{
			chapter = HttpUtility.UrlDecode(chapter);
			if (!System.IO.File.Exists(path))
			{
				return "<p>The book path was not found.</p>";
			}
			var book = EpubReader.Library.EpubReader.ReadBook(path);
			var bookChapter = book.GetAllChapters().SingleOrDefault(c => c.ContentFileName == chapter);
			if (bookChapter == null)
			{
				return $"<p>chapter with filename {chapter} was not found.</p>";
			}
			return bookChapter.HtmlContent + Environment.NewLine +
				"<p><a href=\"" + Url.Action("Read", "Home", new { path = path }) + "\">Volver al índice.</a></p>";
		}



		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
