using MealWise.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MealWise.Services;

namespace NutriSnap.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _database;

    // We use a helper variable to hold the DB file path
    private readonly string _dbPath;

    public DatabaseService()
    {
        // Store the database file in the app's secure local storage directory
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "NutriSnap.db3");
    }

    /// <summary>
    /// Initializes the database connection and creates tables if they don't exist.
    /// </summary>
    private async Task InitAsync()
    {
        if (_database is not null)
            return;

        // Flags to keep the database open, allow read/write, and enable multi-threading
        var flags = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache;

        _database = new SQLiteAsyncConnection(_dbPath, flags);

        // Create tables based on our C# models
        await _database.CreateTableAsync<PantryItem>();
        await _database.CreateTableAsync<MealEntry>();
    }

    // ==========================================
    // PANTRY ITEM METHODS (Tab 2)
    // ==========================================

    public async Task<List<PantryItem>> GetPantryItemsAsync()
    {
        await InitAsync();
        // Returns all items currently in the pantry
        return await _database.Table<PantryItem>().ToListAsync();
    }

    public async Task<int> SavePantryItemAsync(PantryItem item)
    {
        await InitAsync();

        // If Id is not 0, the item already exists in the DB -> Update it
        if (item.Id != 0)
        {
            return await _database.UpdateAsync(item);
        }
        // Otherwise, it's a brand new item -> Insert it
        else
        {
            return await _database.InsertAsync(item);
        }
    }

    public async Task<int> DeletePantryItemAsync(PantryItem item)
    {
        await InitAsync();
        return await _database.DeleteAsync(item);
    }

    // ==========================================
    // MEAL ENTRY METHODS (Tab 3)
    // ==========================================

    /// <summary>
    /// Gets all logged meals for a specific date (usually today).
    /// </summary>
    public async Task<List<MealEntry>> GetDailyMealsAsync(DateTime date)
    {
        await InitAsync();

        // Get start and end of the specified day
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        // Fetch only meals consumed on that specific date
        return await _database.Table<MealEntry>()
            .Where(m => m.DateConsumed >= startOfDay && m.DateConsumed < endOfDay)
            .ToListAsync();
    }

    public async Task<int> SaveMealEntryAsync(MealEntry entry)
    {
        await InitAsync();

        if (entry.Id != 0)
        {
            return await _database.UpdateAsync(entry);
        }
        else
        {
            return await _database.InsertAsync(entry);
        }
    }

    public async Task<int> DeleteMealEntryAsync(MealEntry entry)
    {
        await InitAsync();
        return await _database.DeleteAsync(entry);
    }
}