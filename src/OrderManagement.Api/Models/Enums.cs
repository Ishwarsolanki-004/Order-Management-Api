namespace OrderManagement.Api.Models;

public enum UserRole { Customer = 0, Admin = 1 }

public enum OrderStatus { Pending = 0, Paid = 1, Shipped = 2, Delivered = 3, Cancelled = 4 }
