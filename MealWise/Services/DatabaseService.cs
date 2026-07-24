using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MealWise.Models;
using SQLite;

namespace MealWise.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public DatabaseService()
    {
        // Store the database file in the app's secure local storage directory
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "MealWise.db3");
    }

    /// <summary>
    /// Initializes the database connection and creates tables if they don't exist.
    /// </summary>
    private async Task InitAsync()
    {
        if (_database is not null)
            return;

        var flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache;
        _database = new SQLiteAsyncConnection(_dbPath, flags);

        // Create tables based on C# models
        await _database.CreateTableAsync<PantryItem>();
        await _database.CreateTableAsync<MealEntry>();
    }

    // =========================================================================
    // PANTRY ITEM METHODS (TAB 2)
    // =========================================================================

    /// <summary>
    /// Returns all items currently in the pantry.
    /// </summary>
    public async Task<List<PantryItem>> GetPantryItemsAsync()
    {
        await InitAsync();
        return await _database!.Table<PantryItem>().ToListAsync();
    }

    /// <summary>
    /// Gets a single pantry item by its unique ID.
    /// </summary>
    public async Task<PantryItem?> GetPantryItemByIdAsync(int id)
    {
        await InitAsync();
        return await _database!.Table<PantryItem>().FirstOrDefaultAsync(i => i.Id == id);
    }

    /// <summary>
    /// Inserts or updates a single pantry item.
    /// </summary>
    public async Task<int> SavePantryItemAsync(PantryItem item)
    {
        await InitAsync();

        if (item.Id != 0)
        {
            return await _database!.UpdateAsync(item);
        }

        return await _database!.InsertAsync(item);
    }

    /// <summary>
    /// Efficiently inserts or updates multiple items in a single transaction.
    /// Essential when AI parses multiple items from a photo or receipt.
    /// </summary>
    public async Task SavePantryItemsBatchAsync(IEnumerable<PantryItem> items)
    {
        await InitAsync();
        await _database!.RunInTransactionAsync(tran =>
        {
            foreach (var item in items)
            {
                if (item.Id != 0)
                    tran.Update(item);
                else
                    tran.Insert(item);
            }
        });
    }

    /// <summary>
    /// Removes an item from the pantry database.
    /// </summary>
    public async Task<int> DeletePantryItemAsync(PantryItem item)
    {
        await InitAsync();
        return await _database!.DeleteAsync(item);
    }

    /// <summary>
    /// Wipes all items from the pantry.
    /// </summary>
    public async Task ClearPantryAsync()
    {
        await InitAsync();
        await _database!.DeleteAllAsync<PantryItem>();
    }

    // =========================================================================
    // MEAL ENTRY METHODS (TAB 3 & HISTORICAL ANALYTICS)
    // =========================================================================

    /// <summary>
    /// Gets all logged meals for a specific date (e.g., today).
    /// </summary>
    public async Task<List<MealEntry>> GetDailyMealsAsync(DateTime date)
    {
        await InitAsync();

        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _database!.Table<MealEntry>()
            .Where(m => m.DateConsumed >= startOfDay && m.DateConsumed < endOfDay)
            .ToListAsync();
    }

    /// <summary>
    /// Fetches all logged meals within a specific date range.
    /// Used by NutritionCalculator to calculate 7-day and 30-day trend analytics.
    /// </summary>
    public async Task<List<MealEntry>> GetMealEntriesForPeriodAsync(DateTime startDate, DateTime endDate)
    {
        await InitAsync();

        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        return await _database!.Table<MealEntry>()
            .Where(m => m.DateConsumed >= start && m.DateConsumed < end)
            .ToListAsync();
    }

    /// <summary>
    /// Inserts or updates a meal log entry.
    /// </summary>
    public async Task<int> SaveMealEntryAsync(MealEntry entry)
    {
        await InitAsync();

        if (entry.Id != 0)
        {
            return await _database!.UpdateAsync(entry);
        }

        return await _database!.InsertAsync(entry);
    }

    /// <summary>
    /// Deletes a logged meal entry.
    /// </summary>
    public async Task<int> DeleteMealEntryAsync(MealEntry entry)
    {
        await InitAsync();
        return await _database!.DeleteAsync(entry);
    }
}
