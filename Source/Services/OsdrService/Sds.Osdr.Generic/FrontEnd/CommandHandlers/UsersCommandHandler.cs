using CQRSlite.Domain;
using CQRSlite.Domain.Exception;
using MassTransit;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Users;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.FrontEnd.CommandHandlers
{
    public class UsersCommandHandler : IConsumer<CreateUser>, 
                                       IConsumer<UpdateUser>
    {
        private readonly ISession _session;

        public UsersCommandHandler(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public async Task Consume(ConsumeContext<CreateUser> context)
        {
            try
            {
                await _session.Add(new User(
                    context.Message.Id, context.Message.UserId,
                    firstName: context.Message.FirstName,
                    lastName: context.Message.LastName,
                    displayName: context.Message.DisplayName,
                    loginName: context.Message.LoginName,
                    email: context.Message.Email,
                    avatar: context.Message.Avatar
                ));

                await _session.Commit();
            }
            catch (ConcurrencyException)
            {
            }
        }

        public async Task Consume(ConsumeContext<UpdateUser> context)
        {
            try
            {
                var user = await _session.Get<User>(context.Message.Id);

                user.Update(
                    context.Message.Id,
                    context.Message.UserId,
                    context.Message.NewFirstName,
                    context.Message.NewLastName,
                    context.Message.NewDisplayName,
                    newEmail: context.Message.NewEmail,
                    newAvatar: context.Message.NewAvatar
                );

                await _session.Commit();
            }
            catch (ConcurrencyException)
            {
            }
        }
    }
}

