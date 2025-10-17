using System;
using System.Collections.Generic;

namespace VHS_frontend.Areas.Admin.Models.Dashboard
{
    public class DashboardViewModel
    {
        // Stats Cards
        public decimal TodayRevenue { get; set; }
        public double RevenueChange { get; set; }
        public double RevenueProgress { get; set; }
        
        public int TodayOrders { get; set; }
        public double OrdersChange { get; set; }
        public double OrdersProgress { get; set; }
        
        public int ActiveCustomers { get; set; }
        public double CustomersChange { get; set; }
        public double CustomersProgress { get; set; }
        
        public int ActiveProviders { get; set; }
        public double ProvidersChange { get; set; }
        public double ProvidersProgress { get; set; }
        
        public int ActiveVouchers { get; set; }
        
        public double ConversionRate { get; set; }
        public double ConversionChange { get; set; }
        
        public double AverageRating { get; set; }
        public double RatingChange { get; set; }
        
        // Charts Data
        public List<decimal> RevenueChartData { get; set; } = new List<decimal>();
        public List<string> RevenueChartLabels { get; set; } = new List<string>();
        
        public List<int> OrdersChartData { get; set; } = new List<int>();
        public List<string> OrdersChartLabels { get; set; } = new List<string>();
        
        public List<int> NewCustomersChartData { get; set; } = new List<int>();
        public List<string> NewCustomersChartLabels { get; set; } = new List<string>();
        
        public List<decimal> MonthlyRevenueData { get; set; } = new List<decimal>();
        public List<string> MonthlyRevenueLabels { get; set; } = new List<string>();
        
        public List<int> WeeklyOrdersData { get; set; } = new List<int>();
        public List<string> WeeklyOrdersLabels { get; set; } = new List<string>();
        
        // Service Distribution
        public List<ServiceDistribution> ServiceDistributions { get; set; } = new List<ServiceDistribution>();
        
        // Rating Distribution
        public List<RatingDistribution> RatingDistributions { get; set; } = new List<RatingDistribution>();
        
        // Recent Activities
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
        
        // Provider Registrations
        public List<ProviderRegistration> ProviderRegistrations { get; set; } = new List<ProviderRegistration>();
    }
    
    public class ServiceDistribution
    {
        public string ServiceName { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
    
    public class RatingDistribution
    {
        public int Stars { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
    
    public class RecentActivity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ActivityType { get; set; } // success, warning, info
    }
    
    public class ProviderRegistration
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; }
        public string ServiceDescription { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // pending, approved, rejected
    }
}
