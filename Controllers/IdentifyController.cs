using IdentityLinkingApi.Data;
using IdentityLinkingApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityLinkingApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IdentifyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public IdentifyController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Identify([FromBody] IdentifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return BadRequest("Email or PhoneNumber is required.");
            }

            var matchingContacts = await _context.Contacts
                .Where(c => (request.Email != null && c.Email == request.Email) ||
                            (request.PhoneNumber != null && c.PhoneNumber == request.PhoneNumber))
                .ToListAsync();

            if (matchingContacts.Count == 0)
            {
                var newContact = new Contact
                {
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    LinkPrecedence = "primary",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Contacts.Add(newContact);
                await _context.SaveChangesAsync();

                return Ok(new IdentifyResponse
                {
                    Contact = new ContactResult
                    {
                        PrimaryContactId = newContact.Id,
                        Emails = newContact.Email != null ? new List<string> { newContact.Email } : new List<string>(),
                        PhoneNumbers = newContact.PhoneNumber != null ? new List<string> { newContact.PhoneNumber } : new List<string>(),
                        SecondaryContactIds = new List<int>()
                    }
                });
            }

            var primaryContactIds = matchingContacts
                .Select(c => c.LinkPrecedence == "primary" ? c.Id : c.LinkedId!.Value)
                .Distinct()
                .ToList();

            var allConnectedContacts = await _context.Contacts
                .Where(c => primaryContactIds.Contains(c.Id) || (c.LinkedId != null && primaryContactIds.Contains(c.LinkedId.Value)))
                .ToListAsync();

            allConnectedContacts = allConnectedContacts.OrderBy(c => c.CreatedAt).ToList();

            var oldestPrimary = allConnectedContacts.First();
            bool changesMade = false;

            foreach (var contact in allConnectedContacts)
            {
                if (contact.Id != oldestPrimary.Id && contact.LinkPrecedence == "primary")
                {
                    contact.LinkPrecedence = "secondary";
                    contact.LinkedId = oldestPrimary.Id;
                    contact.UpdatedAt = DateTime.UtcNow;
                    changesMade = true;
                }
                else if (contact.Id != oldestPrimary.Id && contact.LinkedId != oldestPrimary.Id)
                {
                    contact.LinkedId = oldestPrimary.Id;
                    contact.UpdatedAt = DateTime.UtcNow;
                    changesMade = true;
                }
            }

            bool hasNewEmail = !string.IsNullOrWhiteSpace(request.Email) && !allConnectedContacts.Any(c => c.Email == request.Email);
            bool hasNewPhone = !string.IsNullOrWhiteSpace(request.PhoneNumber) && !allConnectedContacts.Any(c => c.PhoneNumber == request.PhoneNumber);

            if (hasNewEmail || hasNewPhone)
            {
                var newSecondary = new Contact
                {
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    LinkedId = oldestPrimary.Id,
                    LinkPrecedence = "secondary",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.Contacts.Add(newSecondary);
                allConnectedContacts.Add(newSecondary);
                changesMade = true;
            }

            if (changesMade)
            {
                await _context.SaveChangesAsync();
            }

            var emails = allConnectedContacts
                .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                .Select(c => c.Email!)
                .Distinct()
                .ToList();

            if (!string.IsNullOrWhiteSpace(oldestPrimary.Email))
            {
                emails.Remove(oldestPrimary.Email);
                emails.Insert(0, oldestPrimary.Email);
            }

            var phones = allConnectedContacts
                .Where(c => !string.IsNullOrWhiteSpace(c.PhoneNumber))
                .Select(c => c.PhoneNumber!)
                .Distinct()
                .ToList();

            if (!string.IsNullOrWhiteSpace(oldestPrimary.PhoneNumber))
            {
                phones.Remove(oldestPrimary.PhoneNumber);
                phones.Insert(0, oldestPrimary.PhoneNumber);
            }

            var secondaryIds = allConnectedContacts
                .Where(c => c.Id != oldestPrimary.Id)
                .Select(c => c.Id)
                .OrderBy(id => id)
                .ToList();

            return Ok(new IdentifyResponse
            {
                Contact = new ContactResult
                {
                    PrimaryContactId = oldestPrimary.Id,
                    Emails = emails,
                    PhoneNumbers = phones,
                    SecondaryContactIds = secondaryIds
                }
            });
        }
    }
}
