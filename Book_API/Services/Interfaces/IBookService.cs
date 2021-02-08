﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Book_API.Services.Interfaces
{
  public interface IBookService
  {
    IActionResult Book(string name);
  }
}
