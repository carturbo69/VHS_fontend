using System;
using System.Collections.Generic;

namespace VHS_frontend.Areas.Customer.Models.ChatDemo;

public partial class Account
{
    public Guid AccountId { get; set; }

    public string AccountName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    //public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

    //public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    //public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    //public virtual Provider? Provider { get; set; }

    //public virtual User? User { get; set; }
}
