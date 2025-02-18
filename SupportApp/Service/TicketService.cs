using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Utilities;
using SupportApp.DTO;
using SupportApp.Models;
using SupportApp.Service.Notifications;
using System;
namespace SupportApp.Service;
public class TicketService
{
    private readonly SupportAppDbContext _context;

    public TicketService(SupportAppDbContext context )
    {
        _context = context;
        
    }
    // private string GenerateTicketNumber()
    // {
    //     
    //     return DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
    // }

    public string GenerateTicketNumber()
    {
        string ticketNumber;
        bool isUnique = false;
        do
        {
            // Generate a new ticket number
            ticketNumber = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
            // Check if the ticket number exists in the database
            isUnique = CheckIfTicketNumberExists(ticketNumber);
        }
        while (!isUnique);
        return ticketNumber;
    }

    private bool CheckIfTicketNumberExists(string ticketNumber)
    {
        // Using database context checking the ticket number
        var existingTicket = _context.Ticket.FirstOrDefault(t => t.TicketNumber == ticketNumber);
        // Return true if no existing ticket is found, false otherwise
        return existingTicket == null;

    }

    // create ticket from mail 
    //public async void CreateTicketFromEmail(EmailBoxService.EmailDetails emailDetails)
    public async Task CreateTicketFromEmail(EmailBoxService.EmailDetails emailDetails)
    {
        var existingTicket = _context.Ticket.FirstOrDefault(ticket => ticket.MessageId == emailDetails.MessageId);
        // Find the "Date" header
        var dateHeader = emailDetails.Headers.FirstOrDefault(header => header.Key == "Date");
        //Console.WriteLine($"This is create ticket from mail , date test : {dateHeader}");
        if (existingTicket == null && DateTime.TryParse(dateHeader.Value, out var createdDate))
        {
            var ticket = new Ticket
            {
                Title = emailDetails.Subject,
                TicketNumber = GenerateTicketNumber(),
                MessageId = emailDetails.MessageId,
                Description = emailDetails.Body,
                Priority = TicketPriority.BusinessClass,
                Attachment = emailDetails.Attachments != null && emailDetails.Attachments.Any()
                    ? string.Join(",", emailDetails.Attachments)
                    : null,
                Status = TicketStatus.Acknowledged,
                CreatedAt = Convert.ToDateTime(createdDate).ToString("yyyy-MM-dd HH:mm:ss"),
                //CreatedAt = createdDate.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                IsEmail = true,
                FromEmail = emailDetails.From.ToString(),
                EmailCc = emailDetails.Cc,
                TicketTypeId = 1,
                CreatedBy = "EMAIL"
            };

            // Format CreatedAt property to "yyyy-MM-dd HH:mm:ss"
            //ticket.CreatedAt = Convert.ToDateTime(createdDate).ToString("yyyy-MM-dd HH:mm:ss");

            _context.Ticket.Add(ticket);
            await _context.SaveChangesAsync();

            int newTicketId = ticket.Id;
            var newTarget = new Target
            {
                TicketId = newTicketId,
                DepartmentId = _context.Department.Where(d => d.DepartmentName == "Information Technology").FirstOrDefault().Id,
                UnitId = _context.Unit.Where(u => u.Name == "Corporate Office").FirstOrDefault().Id,
            };

            _context.Target.Add(newTarget);
            await _context.SaveChangesAsync();
        }
        else
        {
            Console.WriteLine($" This Ticket:'{emailDetails.MessageId}'is already exits.");
        }
    }

    // create ticket from frontend form 
    public async void CreateTicket(TicketAndTargetDto ticketAndTargetDto)
    {
        try
        {
            ticketAndTargetDto.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var generatedTicketNumber = GenerateTicketNumber();
            var ticketData = new Ticket
            {
                Title = ticketAndTargetDto.Title,
                TicketNumber = generatedTicketNumber,
                Description = ticketAndTargetDto.Description,

                // need to save the file on my local ................
                //Attachment = ticketAndTargetDto.Attachment.ToString(),


			    CreatedAt = ticketAndTargetDto.CreatedAt,
                CreatedBy = ticketAndTargetDto.CreatedBy,
                MessageId = generatedTicketNumber,
                Priority = TicketPriority.Regular,
                Status = TicketStatus.Open,
                IsEmail = false,
                TicketTypeId = ticketAndTargetDto.TicketTypeId,
                UpdatedAt = null,
            };


			//---------------------------------------------------------------

			// Attachment handling (assuming attachment object exists in ticketAndTargetDto)
			//         if (ticketAndTargetDto.Attachment != null)
			//         {
			//             // Extract attachment information
			//             var attachment = ticketAndTargetDto.Attachment;
			//             string fileName = ticketAndTargetDto.Attachment.ToString();

			//             // Get the project root folder path
			//             var projectRootPath = Path.Combine(Directory.GetCurrentDirectory()); // Navigate up two levels

			//             // Combine path for "media" subfolder
			//             string folderPath = Path.Combine(projectRootPath, "UploadMedia");

			//             // Check if the folder exists, if not, create it
			//             if (!Directory.Exists(folderPath))
			//             {
			//                 Directory.CreateDirectory(folderPath);
			//             }

			//             // Combine folder path and filename
			//             string filePath = Path.Combine(folderPath, fileName);
			//	//System.IO.File.WriteAllBytes(filePath, attachment);
			//	ticketData.Attachment = filePath;
			//         }

			////---------------------------------------------------------------

			//________________________________________


			

			//________________________________________

			_context.Ticket.Add(ticketData);
            _context.SaveChanges();




            // create target after ticket has been created 
            //int newTicketId = ticketData.Id;
            var newTarget = new Target
            {
                TicketId = ticketData.Id,
                DepartmentId = ticketAndTargetDto.DepartmentId,
                UnitId = ticketAndTargetDto.UnitId,
            };
            _context.Target.Add(newTarget);
            await _context.SaveChangesAsync();

		Console.WriteLine("Create Ticket Successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("This is Service layer error.", ex.Message);
        }
    }


	//private readonly Random _random = new Random();

	//private string GenerateUniqueFilename(string originalFilename)
	//{
	//	// Extract original file extension
	//	string extension = Path.GetExtension(originalFilename);

	//	// Generate a unique timestamped filename with a number postfix
	//	string uniqueFilename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + _random.Next(1000, 9999) + extension;

	//	return uniqueFilename;
	//}


	//--------------------------
	private string SaveAttachment(IFormFile attachment, string fileName)
	{
		// Determine the path to store the file
		var projectRootPath = Path.Combine(Directory.GetCurrentDirectory()); // Or use a different path
		string folderPath = Path.Combine(projectRootPath, "UploadMedia"); // Create a subfolder for organization

		// Ensure the folder exists
		if (!Directory.Exists(folderPath))
		{
			Directory.CreateDirectory(folderPath);
		}

		// Construct the full file path
		string filePath = Path.Combine(folderPath, fileName);

		try
		{
			// Write the file contents to the designated path
			using (Stream stream = new FileStream(filePath, FileMode.Create))
			{
				attachment.CopyTo(stream);
			}

			return filePath; // Return the saved file path for reference
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error saving attachment: " + ex.Message);
			throw; // Re-throw for higher-level handling
		}
	}

	//----------------------------


	public void UpdateTicketstatus(UpdateTicketStatusDto updateTicketStatusDto)
    {
        try {
            var ticketData = _context.Ticket.Where(t => t.Id == updateTicketStatusDto.Id).FirstOrDefault();

            ticketData.Status = updateTicketStatusDto.Status;
            ticketData.Priority = updateTicketStatusDto.Priority;
            ticketData.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            _context.SaveChanges();
            Console.WriteLine("Ticket status update successful.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ticket status update successfull.");
        }
    }

	//public Task<TicketDetailsDto> TicketDetails(int ticketId)
	//{
	//    try {
	//        var ticketDetails = _context.Ticket.Where(t => t.Id == ticketId).FirstOrDefault();
	//        var reviewDetails = _context.Review.Where(r => r.TicketId == ticketId).ToList();

	//        Console.WriteLine("Details Data fetched complete !");
	//        return ticketDetails;
	//    }catch (Exception ex)
	//    {
	//        Console.WriteLine("Ticket Details data fetching false", ex);
	//        return BadRequestResult();
	//    }
	//}



	//------------------------------ AGENT API ----------------------------------


	//Search Ticket by Agent EmpCode
	public async Task<IEnumerable<Ticket>> GetAgentIssueListAsync(string agentId)
	{
		var agentData = await _context.Agent.FirstOrDefaultAsync(agent => agent.EmpCode == agentId);
		if (agentData == null)
		{
			throw new ArgumentException("No data for this agent.", nameof(agentId));
		}

		var ticketDetailsData = await _context.Target
			.Include(t => t.Ticket)
			.Where(t => t.AgentId == agentData.AgentId)
			.Select(t => t.Ticket)
			.ToListAsync();

		if (ticketDetailsData == null || ticketDetailsData.Count == 0)
		{
			throw new ArgumentException("No ticket details found for this agent.", nameof(agentId));
		}

		return ticketDetailsData;
	}


    //------------------------------ EMPLOYEE API ----------------------------------


    //get ticket list by ticket creator ID

    public async Task<IEnumerable<Ticket>> GetAcknowledgeTicketListByCreatorAsync(string EmpCode)
    {
        var empCodeParam = new SqlParameter("@EmpCode", EmpCode);
        return await _context.Ticket.FromSqlRaw(
            "SELECT ticket. * FROM Ticket ticket left JOIN Target target ON ticket.Id = target.TicketId  WHERE ticket.CreatedBy = @EmpCode AND target.AgentId IS NOT NULL;",
            empCodeParam).ToListAsync();

    }

    //public async Task<IEnumerable<SingleIssueDetailsDto>> GetAcknowledgeTicketListByCreatorAsync(string EmpCode) {
    //    var empCodeParam = new SqlParameter("@EmpCode", EmpCode);
    //    var ticketsWithAgentInfo = await _context.Ticket
    //        .FromSqlRaw(
    //            "SELECT ticket.*, agent.Name as AgentName, agent.PhoneExtension as PhoneEXT FROM Ticket ticket JOIN Target target ON ticket.Id = target.TicketId LEFT JOIN Agent agent ON target.AgentId = agent.AgentId WHERE ticket.CreatedBy = @EmpCode AND target.AgentId IS NOT NULL;",
    //            empCodeParam)
    //        .Select(ticket => new SingleIssueDetailsDto {
    //            Id = ticket.Id,
    //            Title = ticket.Title,
    //            Description = ticket.Description,
    //            IsEmail = ticket.IsEmail,
    //            FromEmail = ticket.FromEmail,
    //            UpdatedBy = ticket.UpdatedBy,
    //            CreatedAt = ticket.CreatedAt,
    //            CreatedBy = ticket.CreatedBy,
    //            Status = ticket.Status,
    //            Priority = ticket.Priority,
    //            TicketType = ticket.TicketType,
    //            AgentName = ticket.AgentName,
    //            PhoneEXT = ticket.PhoneEXT
    //        })
    //        .ToListAsync();

    //    return ticketsWithAgentInfo;
    //}





    public async Task<IEnumerable<Ticket>> GetRecentRaisedTicketListByCreatorAsync(string EmpCode)
	{
		var empCodeParam = new SqlParameter("@EmpCode", EmpCode);

        return await _context.Ticket.FromSqlRaw("SELECT ticket.*  FROM Ticket ticket LEFT JOIN Target target ON ticket.Id = target.TicketId  WHERE ticket.CreatedBy = @EmpCode AND target.AgentId IS NULL;",

            empCodeParam).ToListAsync();
	}



    // pagination API for tickets

	public IEnumerable<Ticket> GetPaginationList(int currentPage, int pageSize)
	{
		int skip = (currentPage - 1) * pageSize;
		return _context.Ticket.OrderByDescending(t => t.CreatedAt).Where(t => (int)t.Status < 5)
							   .Skip(skip)
							   .Take(pageSize)
							   .ToList();
	}

	// update for check ................

	public async Task<string> UpdateForCheckTicketStatus(int ticketId)
	{
		try
		{
			var ticketToUpdate = await _context.Ticket
				.Where(t => t.Id == ticketId)
				.SingleOrDefaultAsync();

			if (ticketToUpdate != null)
			{
				ticketToUpdate.Status++;
				_context.Ticket.Update(ticketToUpdate);
				await _context.SaveChangesAsync();

				return "Ticket Status Updated.";
			}
			else
			{
				return "Ticket not found.";
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			return "Failed to update ticket status.";
		}
	}




	// get mail ticket list 

	public IEnumerable<Ticket> GetMailTicketList(int currentPage, int pageSize)
	{
		try
		{
			int skip = (currentPage - 1) * pageSize;
			return  _context.Ticket.Where(t => t.IsEmail == true && (int)t.Status < 4)
						.OrderByDescending(t => t.CreatedAt)
						.Skip(skip)
						.Take(pageSize)
						.ToList();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			return Enumerable.Empty<Ticket>();
		}
	}

	// add soft reminder to an agent ..............

	public async Task<string> Softreminder(int ticketId)
	{
		try

            //_context.Target.FirstOrDefault(t => t.TicketId == ticketId);
        {
			var targetData = await _context.Target.FirstOrDefaultAsync(t => t.TicketId == ticketId);

			if (targetData != null)
			{
				var notificationToUpdate = await _context.Notification.FirstOrDefaultAsync(n => n.TargetId == targetData.Id);

				if (notificationToUpdate != null)
				{
					notificationToUpdate.IsRead = true;
                    
					await _context.SaveChangesAsync();

					return ("Notification marked as read successfully.");
				}
				else
				{
					return ("No notification found for the specified target.");
				}
			}
			else
			{
				return ("Target not found for the ticket ID.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			return ("Internal server error."); 
		}
	}


	//------------------------------ Agent API ----------------------------------


	//------------------------------ ADMIN API ----------------------------------
}