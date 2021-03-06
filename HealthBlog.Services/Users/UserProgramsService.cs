﻿namespace HealthBlog.Services.Users
{
	using AutoMapper;
	using Microsoft.AspNetCore.Identity;
	using Microsoft.EntityFrameworkCore;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Contracts;
	using HealthBlog.Common.Users.ViewModels;
	using HealthBlog.Data;
	using HealthBlog.Models;
	using HealthBlog.Common.Exceptions;
	using HealthBlog.Common.Users.BindingModels;
	using HealthBlog.Common.Trainers.BindingModels;
	using HealthBlog.Common;

	public class UserProgramsService : BaseProgramService, IUserProgramsService
	{
		private const string ownedName = "Вие";

		public UserProgramsService(
			HealthBlogDbContext dbContext,
			IMapper mapper,
			UserManager<User> userManager)
			: base(dbContext, mapper, userManager)
		{
		}

		public async Task<IEnumerable<ProgramForBuyingVewModel>> GetProgramsForBuyingAsync(string username, string searchTerm)
		{
			var user = await this.DbContext.Users
				.Include(u => u.CreatedPrograms)
				.Include(u => u.OwnedPrograms)
				.FirstOrDefaultAsync(u => u.UserName == username);

			if (user == null)
			{
				throw new InvalidUserException();
			}

			var ownedProgramIds = user.OwnedPrograms
					.Select(op => op.ProgramId)
					.ToList();
			ownedProgramIds.AddRange(
				user.CreatedPrograms
					.Select(cp => cp.Id));

			IEnumerable<ProgramForBuyingVewModel> model = null;
			if (searchTerm == null)
			{
				model = this.Mapper.Map<IEnumerable<ProgramForBuyingVewModel>>(
				 this.DbContext.Programs
					.Include(p => p.Author)
					.Include(p => p.Days)
					.Where(p => p.IsForSale)
					.Where(p => !ownedProgramIds
						.Any(opId => opId == p.Id)));
			}
			else
			{
				model = this.Mapper.Map<IEnumerable<ProgramForBuyingVewModel>>(
				 this.DbContext.Programs
				 .Include(p => p.Author)
				 .Include(p => p.Days)
					.Where(p => p.IsForSale)
					.Where(p => !ownedProgramIds
						.Any(opId => opId == p.Id))
					.Where(p =>
						p.Name.Contains(searchTerm) ||
						p.Type.Contains(searchTerm) ||
						p.Author.UserName.Contains(searchTerm) ||
						p.Author.FullName.Contains(searchTerm)));
			}

			return model;
		}

		public async Task BuyProgramAsync(int programId, string username)
		{
			var user = await this.GetUserByNamedAsync(username);

			if (!await this.IsValidProgram(programId, user.Id))
			{
				throw new InvalidProgramException();
			}

			user.OwnedPrograms
				.Add(new UserProgram()
				{
					ProgramId = programId
				});
			await this.DbContext.SaveChangesAsync();
		}

		public async Task<ICollection<ProgramsIndexViewModel>> GetOwnedAndCreatedProgramsAsync(string username)
		{
			var user = (await this.DbContext.Users
				.Include(p => p.CreatedPrograms)
					.ThenInclude(cp => cp.Days)
				.Include(p => p.OwnedPrograms)
					.ThenInclude(p => p.Program)
						.ThenInclude(p => p.Author)
				.Include(p => p.OwnedPrograms)
					.ThenInclude(p => p.Program)
						.ThenInclude(p => p.Days)
				.FirstOrDefaultAsync(p => p.UserName == username));

			CoreValidator.ThrowIfNull(user, new InvalidUserException());

			var model = this.Mapper.Map<List<ProgramsIndexViewModel>>(
				user.CreatedPrograms
					.Where(p => p.Name != defaultUserProgramName));
			model.ForEach(m => m.Author = ownedName);

			model.AddRange(
				this.Mapper.Map<IEnumerable<ProgramsIndexViewModel>>(
					user.OwnedPrograms));

			return model;
		}

		public async Task<ProgramDetailsViewModel> GetProgramDetailsAsync(string username, int programId)
		{
			if (!await this.ValidateProgramByIdAsync(programId, username))
			{
				throw new InvalidProgramException();
			}

			var program = await this.DbContext.Programs
					.Include(p => p.Author)
					.Include(p => p.Days)
						.ThenInclude(pd => pd.Day.Meals)
					.Include(p => p.Days)
						.ThenInclude(pd => pd.Day.Trainings)
					.FirstOrDefaultAsync(p => p.Id == programId);

			var model = this.Mapper.Map<ProgramDetailsViewModel>(program);
			model.IsCreatedByCurrentUser = program.Author.UserName == username;

			return model;
		}

		public async Task<bool> ValidateProgramByIdAsync(int programId, string username)
		{
			var user = (await this.DbContext.Users
				.Include(u => u.CreatedPrograms)
				.Include(u => u.OwnedPrograms)
				.FirstOrDefaultAsync(u => u.UserName == username));

			CoreValidator.ThrowIfNull(user, new InvalidUserException());

			bool isCreatedProgram = user.CreatedPrograms.Any(p => p.Id == programId);
			bool isOwnedProgram = user.OwnedPrograms.Any(p => p.ProgramId == programId);

			return isOwnedProgram || isCreatedProgram;
		}

		public async Task<Program> GetDefaulttUserProgram(string userId)
		{
			var user = await this.UserManager.FindByIdAsync(userId);

			if (!await this.DbContext.Programs
				.AnyAsync(p => p.AuthorId == userId && p.Name == defaultUserProgramName))
			{
				await this.CreateProgramAsync(new ProgramCreateBindingModel()
				{
					Name = defaultUserProgramName,
					Type = defaultUserProgramType,
					Description = defaultUserProgramDescription
				},
				user.UserName);
			}

			var program = await this.DbContext.Programs
				.Include(p => p.Days)
				.ThenInclude(pd => pd.Day)
				.FirstOrDefaultAsync(p => p.AuthorId == userId && p.Name == defaultUserProgramName);

			return program;
		}

		private async Task<bool> IsValidProgram(int programId, string userId)
		{
			return await this.DbContext.Programs
				.AnyAsync(p => p.Id == programId && p.AuthorId != userId);
		}
	}
}
