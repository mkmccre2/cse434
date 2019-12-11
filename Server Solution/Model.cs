using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;

namespace ServerEF
{
    public class SessionContext : DbContext
    {
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Subscriber> Subscribers { get; set; }
        public DbSet<Following> Followings { get; set; }
        public DbSet<Post> Posts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite("Data Source=/Users/marymccready/Code/ServerEF/session.db");
    }

    public class Session
    {
        public int SessionId { get; set; }
        public Client Client { get; set; }
        public DateTime TimeNow { get; set; }
        public int token { get; set; }
        public int state { get; set; }
    }

    public class Client
    {
        public int ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<Post> Posts { get; } = new List<Post>();
        public List<Subscriber> Subscribers { get; } = new List<Subscriber>();
        public List<Following> Followings { get; } = new List<Following>();

        public int SessionId { get; set; }
        public Session Session { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        public string Content { get; set; }

        public int ClientId { get; set; }
        public Client Client { get; set; }
    }

    public class Subscriber
    {
        public int SubscriberId { get; set; }
        public string SubscriberName { get; set; }

        public int ClientId { get; set; }
        public Client Client { get; set; }
    }

    public class Following
    {
        public int FollowingId { get; set; }
        public string FollowingName { get; set; }

        public int ClientId { get; set; }
        public Client Client { get; set; }
    }
}
