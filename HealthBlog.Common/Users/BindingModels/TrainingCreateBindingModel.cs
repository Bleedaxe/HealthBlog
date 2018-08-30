﻿namespace HealthBlog.Common.Users.BindingModels
{
	using System.ComponentModel.DataAnnotations;

	using Constants;

	public class TrainingCreateBindingModel
	{
		[Required]
		[StringLength(30, MinimumLength = 2)]
		[Display(Name = AttributeDisplayNameConstants.Name)]
		public string Name { get; set; }

		[Required]
		[StringLength(30, MinimumLength = 2)]
		[Display(Name = AttributeDisplayNameConstants.Type)]
		public string Type { get; set; }

		[Required]
		[StringLength(200, MinimumLength = 2)]
		[Display(Name = AttributeDisplayNameConstants.Description)]
		public string Description { get; set; }
	}
}
