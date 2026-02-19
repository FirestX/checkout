using CheckOut.Data;
using CheckOut.Models;
using LinqToDB;
using LinqToDB.Async;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace CheckOut.Services;

public class TeacherService(AppDataContext db)
{
	public async Task<int> CreateTeacher(Payload payload)
	{
		var teacher = new Teacher
		{
			GoogleId = payload.Subject,
			Email = payload.Email,
			FullName = payload.Name ?? payload.Email
		};

		return await db.InsertWithInt32IdentityAsync(teacher);
	}

	public async Task<Teacher?> GetTeacherAsync(int id)
	{
		return await db.Teachers.FirstOrDefaultAsync(t => t.Id == id);
	}

	public async Task<Teacher?> GetTeacherAsync(string googleId)
	{
		return await db.Teachers.FirstOrDefaultAsync(t => t.GoogleId == googleId);
	}

	public async Task UpdateTeacherAsync(Teacher teacher)
	{
		await db.UpdateAsync(teacher);
	}

	public bool IsInfoOutdated(Teacher teacher, Payload payload)
	{
		return teacher.Email != payload.Email || teacher.FullName != payload.Name;
	}
}