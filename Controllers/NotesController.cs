using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using net5_webapi.Engines;
using System.Security.Claims;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace net5_webapi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly IDBEngine db;

        public NotesController(IDBEngine DBEngine)
        {
            db = DBEngine;
        }

        /// <summary>
        /// Returns all the Notes for this user.
        /// </summary>
        // GET: api/Notes
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            string user = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Ok(await db.JsonArray("SELECT * FROM Notes WHERE userId=@user", new { user }));
        }

        /// <summary>
        /// Returns a specific Note (id) for this user.
        /// </summary>
        // GET api/Notes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(int id)
        {
            string user = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Ok(await db.Json("SELECT * FROM Notes WHERE userId=@user AND ID=@id", new { user, id }));
        }

        public class NoteBody
        {
            [Required]
            public string Text { get; set; }
        }

        /// <summary>
        /// Creates a new Note for this user.
        /// </summary>
        // POST api/Notes
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] NoteBody body)
        {
            string user = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Ok(await db.Value<int>("INSERT INTO Notes (text, date, userId) VALUES (@text, GETDATE(), @user); SELECT SCOPE_IDENTITY()", new { body.Text, user }));
        }

        /// <summary>
        /// Updates a specific Note (id) for this user.
        /// </summary>
        // PUT api/Notes/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromBody] NoteBody body)
        {
            string user = User.FindFirstValue(ClaimTypes.NameIdentifier);

            int found = await db.Value<int>("SELECT COUNT(*) FROM Notes WHERE ID=@id AND userID=@user;UPDATE Notes SET text=@text WHERE ID=@id AND userId=@user", new { body.Text, id, user });

            if (found > 0)
                return Ok("UPDATED: " + id);
            else
                return Unauthorized();
        }

        /// <summary>
        /// Deletes a specific Note (id) for this user.
        /// </summary>
        // DELETE api/Notes/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            string user = User.FindFirstValue(ClaimTypes.NameIdentifier);

            int found = await db.Value<int>("SELECT COUNT(*) FROM Notes WHERE ID=@id AND userID=@user;DELETE Notes WHERE ID=@id AND userId=@user", new { id, user });

            if (found > 0)
                return Ok("DELETED: " + id);
            else
                return Unauthorized();
        }
    }
}
