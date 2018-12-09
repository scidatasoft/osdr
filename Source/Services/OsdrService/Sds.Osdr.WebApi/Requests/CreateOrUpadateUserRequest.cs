using System;

namespace Sds.Osdr.WebApi.Requests
{
    public class CreateOrUpdateUserRequest
    {
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LoginName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
    }
}
