﻿using Book_API.Services.Interfaces;
using Book_API.Extensions;
using Book_EF.EntityModels;
using BookDBAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Book_API.Services
{
    public class BookService : IBookService
  {
    private readonly BookContext _dbContext;

    public BookService(BookContext dbContext)
    {
      _dbContext = dbContext;
    }

        /// <summary>
        /// 全件カウント
        /// </summary>
        /// <returns>処理結果</returns>
        public IActionResult Count()
    {
      var count = this._dbContext.Book.Count();
      return new OkObjectResult(count);
    }

    /// <summary>
    /// 検索
    /// </summary>
    /// <param name="searchKey">検索条件</param>
    /// <returns>処理結果</returns>
    public IActionResult GetBookItems(BookItemSearchKey searchKey)
    {
            var data = this._dbContext.Book
                .WhereIf(searchKey.From.HasValue, x => x.Date >= searchKey.From)
                .WhereIf(searchKey.To.HasValue, x => x.Date <= searchKey.To)
                .WhereIf(!string.IsNullOrEmpty(searchKey.Title), x => x.Title.Contains(searchKey.Title))
                .WhereIf(!string.IsNullOrEmpty(searchKey.PublishYear), x => x.PublishYear == searchKey.PublishYear)
                .WhereIf(searchKey.RecommendFlg != 0, x => x.RecommendFlg == searchKey.RecommendFlg.ToString())
                .Join(this._dbContext.Author
                    , b => b.AuthorCd
                    , a => a.AuthorCd
                    , (b, a) => new { Book = b, Author =a })
                .WhereIf(!string.IsNullOrEmpty(searchKey.Author), x => x.Author.AuthorName.Contains(searchKey.Author))
                .Join(this._dbContext.Publisher
                    , b => b.Book.PublisherCd
                    , p => p.PublisherCd
                    , (b, p) => new { b.Book, b.Author, Publisher = p })
                .WhereIf(!string.IsNullOrEmpty(searchKey.Publisher), x => x.Publisher.PublisherName.Contains(searchKey.Publisher))
                .Join(this._dbContext.Class
                    , b => b.Book.ClassCd
                    , c => c.ClassCd
                    , (b, c) => new { b.Book, b.Author, b.Publisher, Class = c })
                .WhereIf(!string.IsNullOrEmpty(searchKey.Class), x => x.Class.ClassName.Contains(searchKey.Class))
                .OrderBy(x => x.Book.Date)
            .Select(x => new BookItem
             {
                 Autonumber = x.Book.Autonumber,
                 DateTime = x.Book.Date.Value,
                 Title = x.Book.Title ?? string.Empty,
                 AuthorCd = x.Book.AuthorCd,
                 Author = x.Author.AuthorName ?? string.Empty,
                 PublisherCd = x.Book.PublisherCd,
                 Publisher = x.Publisher.PublisherName ?? string.Empty,
                 ClassCd = x.Book.ClassCd,
                 Class = x.Class.ClassName ?? string.Empty,
                 PublishYear = x.Book.PublishYear ?? string.Empty,
                 PageCount = x.Book.PageCount,
                 RecommendFlg = x.Book.RecommendFlg ?? string.Empty,
             })
            .ToArray();

            return new OkObjectResult(data);
    }

    /// <summary>
    /// 本データの登録
    /// </summary>
    /// <param name="data">データ</param>
    /// <returns>処理結果</returns>
    public IActionResult InsertData(List<BookItem> data)
    {
        var num = 0;

        foreach (var rec in data)
        {
            using (var tran = _dbContext.Database.BeginTransaction())
            {
                var authorCd = InsertAuthor(rec);
                var publisherCd = InsertPublisher(rec);
                var classCd = InsertClass(rec);
                num = InsertBook(rec, authorCd, publisherCd, classCd);
                this._dbContext.SaveChanges();
                tran.Commit();
            }
        }

        return new OkObjectResult(num);
    }

    /// <summary>
    /// 著者の登録
    /// </summary>
    /// <param name="data">データ</param>
    private int InsertAuthor(BookItem data)
    {
      var cd = 0;

      var author = this._dbContext.Author.FirstOrDefault(x => x.AuthorName == data.Author);

      if (author == null)
      {
        cd = this._dbContext.Author.Max(x => x.AuthorCd) + 1;

        var entity = new Author
        {
          AuthorCd = cd,
          AuthorName = data.Author,
        };

        this._dbContext.Author.Add(entity);
      }
      else
      {
        cd = author.AuthorCd;
      }

      return cd;
    }

    /// <summary>
    /// 出版社の登録
    /// </summary>
    /// <param name="data">データ</param>
    private int InsertPublisher(BookItem data)
    {
      var cd = 0;

      var publisher = this._dbContext.Publisher.FirstOrDefault(x => x.PublisherName == data.Publisher);

      if (publisher == null)
      {
        cd = this._dbContext.Publisher.Max(x => x.PublisherCd) + 1;

        var entity = new Publisher
        {
          PublisherCd = cd,
          PublisherName = data.Publisher,
        };

        this._dbContext.Publisher.Add(entity);
      }
      else
      {
        cd = publisher.PublisherCd;
      }

      return cd;
    }

    /// <summary>
    /// 分類の登録
    /// </summary>
    /// <param name="data">データ</param>
    private int InsertClass(BookItem data)
    {
      var cd = 0;

      var classData = this._dbContext.Class.FirstOrDefault(x => x.ClassName == data.Class);

      if (classData == null)
      {
        cd = this._dbContext.Class.Max(x => x.ClassCd) + 1;

        var entity = new Class
        {
          ClassCd = cd,
          ClassName = data.Class,
        };

        this._dbContext.Class.Add(entity);
      }
      else
      {
        cd = classData.ClassCd;
      }

      return cd;
    }

    /// <summary>
    /// 本の登録
    /// </summary>
    /// <param name="data">データ</param>
    /// <param name="authorCd">著者コード</param>
    /// <param name="publisherCd">出版社コード</param>
    /// <param name="classCd">分類コード</param>
    private int InsertBook(BookItem data, int authorCd, int publisherCd, int classCd)
    {
      var autoNum = 0;

      var book = this._dbContext.Book.FirstOrDefault(x => x.Date == data.DateTime && x.Title == data.Title);

      if (book == null)
      {
        autoNum = this._dbContext.Book.Max(x => x.Autonumber) == 0 ? 1 : this._dbContext.Book.Max(x => x.Autonumber) + 1;

        var entity = new Book
        {
          Autonumber = autoNum,
          Date = data.DateTime,
          Title = data.Title,
          AuthorCd = authorCd,
          PublisherCd = publisherCd,
          ClassCd = classCd,
          PublishYear = data.PublishYear,
          PageCount = data.PageCount,
          RecommendFlg = data.RecommendFlg,
          DeleteFlg = "0",
        };

        this._dbContext.Book.Add(entity);
      }
      return autoNum;
    }

    /// <summary>
    /// 本の更新
    /// </summary>
    /// <param name="data">データ</param>
    public IActionResult UpdateData(BookItem data)
    {
      var target = this._dbContext.Book.FirstOrDefault(x => x.Autonumber == data.Autonumber);
      target.Date = data.DateTime;
      target.Title = data.Title;
      target.AuthorCd = data.AuthorCd;
      target.PublisherCd = data.PublisherCd;
      target.ClassCd = data.ClassCd;
      target.PageCount = data.PageCount;
      target.PublishYear = data.PublishYear;
      target.RecommendFlg = data.RecommendFlg;
      _dbContext.Update(target);
      _dbContext.SaveChanges();

      return new OkResult();
    }

        /// <summary>
        /// 本の削除
        /// </summary>
        /// <param name="autoNumber">削除対象データ</param>
        public IActionResult DeleteData(int autoNumber)
        {
            var target = this._dbContext.Book.FirstOrDefault(x => x.Autonumber == autoNumber);
            _dbContext.Remove(target);
            _dbContext.SaveChanges();

            return new OkResult();
        }
    }
}