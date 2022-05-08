using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ElevenNote.Models.Note;
using ElevenNote.Data;
using Microsoft.EntityFrameworkCore;
using ElevenNote.Data.Entities;
using ElevenNote.Models;


namespace ElevenNote.Services.Note
{
    public class NoteService : INoteService
    {
        private readonly int _userId;
        private readonly ApplicationDbContext _dbContext;
        public NoteService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext dbContext)
        {
            var userClaims = httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity;
            var value = userClaims.FindFirst("Id")?.Value;
            var validId = int.TryParse(value, out _userId);

            if(!validId)
            throw new Exception("Attempted to build NoteService without User Id claim.");

            _dbContext = dbContext;
        }
        public async Task<bool> CreateNoteAsync(NoteCreate request)
        {
            var noteEntity = new NoteEntity
            {
                Title = request.Title,
                Content = request.Content,
                CreatedUtc = DateTimeOffset.Now,
                OwnerId = _userId
            };
            _dbContext.Notes.Add(noteEntity);
            var numberOfChanges = await _dbContext.SaveChangesAsync();
            return numberOfChanges == 1;
        }
        public async Task<IEnumerable<NoteListItem>> GetAllNotesAsync()
        {
            var notes = await _dbContext.Notes
                .Where(entity => entity.OwnerId == _userId)
                .Select(entity => new NoteListItem
                {
                    Id = entity.Id,
                    Title = entity.Title,
                    Content = entity.Content,
                    CreatedUtc = entity.CreatedUtc
                })
                .ToListAsync();
                return notes;
        }
        public async Task<NoteDetail> GetNoteByIdAsync(int noteId)
        {
            // Find the first note that has the given Id and an OwnerId that match the requesting userId
                var noteEntity = await _dbContext.Notes
                .FirstOrDefaultAsync(e =>
                    e.Id == noteId && e.OwnerId == _userId
                    );
            // If noteEntity is null then return null, otherwise initialize and return a new NoteDetail
                return noteEntity is null ? null : new NoteDetail
                {
                    Id = noteEntity.Id,
                    Title = noteEntity.Title,
                    Content = noteEntity.Content,
                    CreatedUtc = noteEntity.CreatedUtc,
                    ModifiedUtc = noteEntity.ModifiedUtc
                };
            }
        }
}