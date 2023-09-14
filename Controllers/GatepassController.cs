﻿using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;


namespace Document_Management.Controllers
{
    public class GatepassController : Controller
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}

        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        //Passing the dbcontext in to another variable
        public GatepassController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        //RequestGatepass
        [HttpGet]
        public IActionResult Insert()
        {
            var username = HttpContext.Session.GetString("username");
            if (!string.IsNullOrEmpty(username))
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        public IActionResult Insert(RequestGP gpInfo)
        {
            var username = HttpContext.Session.GetString("username");

            if (ModelState.IsValid)
            {
                if (username != null)
                {
                    gpInfo.Username = username;
                    }
                    _dbcontext.Gatepass.Add(gpInfo);
                _dbcontext.SaveChanges();
                return RedirectToAction("Insert");
            }

            return View(gpInfo);
        }



        public IActionResult Validator()
        {
            var username = HttpContext.Session.GetString("userrole")?.ToLower();
            if (!(username == "validator"))
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact MIS Department.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            ViewBag.users = _dbcontext.Gatepass.ToList();

            return View();
        }

        [HttpGet]
        public IActionResult Approved(int? id)
        {
            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id);


            if (requestGP == null)
            {
                return NotFound();
            }

            return View(requestGP);
        }

        [HttpPost, ActionName("Approved")]
        public async Task<IActionResult> Approved(int id)
        {
            if (_dbcontext.Gatepass == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Account'  is null.");
            }
            var client = await _dbcontext.Gatepass.FindAsync(id);
            //if (client != null)
            //{
            //    _dbcontext.Gatepass.Remove(client);
            //}

            if (client != null)
            {
                client.Status = "Approved";
                await _dbcontext.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Validator));
        }



        [HttpGet]
        public IActionResult Disapproved(int? id)
        {
            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id);

            if (requestGP == null)
            {
                return NotFound();
            }

            return View(requestGP);
        }


        [HttpPost, ActionName("Disapproved")]
        public async Task<IActionResult> Disapproved(int id)
        {
            if (_dbcontext.Gatepass == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Account'  is null.");
            }
            var client = await _dbcontext.Gatepass.FindAsync(id);
            //if (client != null)
            //{
            //    _dbcontext.Gatepass.Remove(client);
            //}

            if (client != null)
            {
                client.Status = "Disapproved";
                await _dbcontext.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Validator));
        }

        [HttpGet]
        public IActionResult RecievedGP()
        {

            var username = HttpContext.Session.GetString("username");

            if (!string.IsNullOrEmpty(username))
            {
                ViewBag.users = _dbcontext.Gatepass.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet]
        public IActionResult Generate(int id)
        {

            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id);

            if (requestGP == null)
            {
                return NotFound();
            }

            return View(requestGP);
        }

    }
}
