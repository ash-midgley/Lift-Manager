﻿using TrainingManager.Models;
using TrainingManager.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace TrainingManager.Controllers
{
    public class PlansController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public PlansController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public ActionResult Index(string query = null)
        {
            var view = new View
            {
                Time = DateTime.Now
            };
            _unitOfWork.Views.Add(view);
            _unitOfWork.Complete();

            var plansToDisplay = _unitOfWork.Plans.GetAllPlans();

            if (!String.IsNullOrWhiteSpace(query))
            {
                plansToDisplay = _unitOfWork.Plans.GetFilteredPlans(query);
            }
            
            // In UI we display ratings as integers
            foreach (Plan plan in plansToDisplay)
            {
                plan.Rating = Math.Round(plan.Rating, 0);
            }

            var userId = User.Identity.GetUserId();
            var favourites = _unitOfWork.Favourites.GetUserFavourites(userId)
                .ToLookup(f => f.PlanId);

            var ratings = _unitOfWork.Ratings.GetUserRatings(userId)
                .ToLookup(r => r.PlanId);

            var viewModel = new PlansViewModel
            {
                PlansToDisplay = plansToDisplay
                .OrderByDescending(p => p.Rating)
                .ThenByDescending(p => p.RatingCount)
                .ThenByDescending(p => p.DateCreated),
                ShowActions = User.Identity.IsAuthenticated,
                Heading = "User Plans",
                Favourites = favourites,
                Ratings = ratings,
                SearchTerm = query,
                UserId = userId
            };

            return View("Index", viewModel);
        }

        [Authorize]
        public ActionResult Create()
        {
            var viewModel = new PlansFormViewModel
            {
                Heading = "Add a Plan"
            };
            return View("PlanForm", viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PlansFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View("PlanForm", viewModel);
            }

            var plan = new Plan
            {
                UserId = User.Identity.GetUserId(),
                Name = viewModel.Name,
                LengthInWeeks = viewModel.LengthInWeeks,
                DateCreated = DateTime.UtcNow,
                Workouts = viewModel.Workouts,
                Rating = 0,
                RatingCount = 0
            };

            _unitOfWork.Plans.Add(plan);
            _unitOfWork.Complete();

            return RedirectToAction("Mine");
        }

        [Authorize]
        public ActionResult Edit(int id)
        {
            var userId = User.Identity.GetUserId();
            var plan = _unitOfWork.Plans.GetUserPlan(id, userId);

            var viewModel = new PlansFormViewModel
            {
                Id = plan.Id,
                Name = plan.Name,
                LengthInWeeks = plan.LengthInWeeks,
                Heading = "Edit Plan"
            };

            return View("PlanForm", viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(PlansFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View("PlanForm", viewModel);
            }

            var userId = User.Identity.GetUserId();
            var plan = _unitOfWork.Plans.GetUserPlan(viewModel.Id, userId);

            plan.Name = viewModel.Name;

            _unitOfWork.Complete();

            return RedirectToAction("Mine", "Plans");
        }

        [Authorize]
        public ActionResult Mine()
        {
            var userId = User.Identity.GetUserId();
            var plansToDisplay = _unitOfWork.Plans.GetUserPlans(userId);

            //In UI we display rating as integer
            foreach (Plan plan in plansToDisplay)
            {
                plan.Rating = Math.Round(plan.Rating, 0);
            }

            var viewModel = new PlansViewModel
            {
                PlansToDisplay = plansToDisplay,
                ShowActions = false,
                Heading = "My Plans",
                Favourites = null
            };

            return View("Mine", viewModel);

        }

        [Authorize]
        public ActionResult Favourites()
        {
            var userId = User.Identity.GetUserId();
            var plans = _unitOfWork.Plans.GetFavouritePlans(userId);

            //In UI we display rating as integer
            foreach (Plan plan in plans)
            {
                plan.Rating = Math.Round(plan.Rating, 0);
            }

            var viewModel = new PlansViewModel
            {
                PlansToDisplay = plans,
                ShowActions = false,
                Heading = "My Favourites",
                Favourites = null
            };

            return View("Favourites", viewModel);
        }

        [Authorize]
        public ActionResult Ratings()
        {
            var userId = User.Identity.GetUserId();
            var ratings = _unitOfWork.Ratings.GetUserRatings(userId);

            var viewModel = new RatingsViewModel
            {
                RatingsToDisplay = ratings,
                ShowActions = true,
                Heading = "My Ratings",
                UserId = User.Identity.GetUserId()
            };

            return View("Ratings", viewModel);
        }

        public ActionResult Summary(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }

            _unitOfWork.Plans.IncrementPlanViews(id.Value);
            _unitOfWork.Complete();

            var plan = _unitOfWork.Plans.GetPlan(id.Value);

            var viewModel = new PlansFormViewModel
            {
                Name = plan.Name,
                PlanId = id.Value,
                LengthInWeeks = plan.LengthInWeeks,
                CreatedOn = plan.DateCreated.ToShortDateString(),
                Rating = _unitOfWork.Ratings.GetRatingAverage(id.Value),
                RatingCount = _unitOfWork.Ratings.GetRatingCount(id.Value),
                Favourites = _unitOfWork.Favourites.GetNumberOfFavourites(id.Value),
                Views = plan.Views,
                CanEdit = (User.Identity.GetUserId() == plan.UserId)
            };

            return View("Summary", viewModel);
        }

        [HttpPost]
        public ActionResult Search(PlansViewModel viewModel)
        {
            return RedirectToAction("Index", "Plans", new { query = viewModel.SearchTerm });
        }
    }
}