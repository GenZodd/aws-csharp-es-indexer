// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InventoryItem.cs" company="Outsell, LLC.">
//   Copyright © Outsell, LLC., All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Outsell.Dep.Services.Inventory
{
    using Amazon.DynamoDBv2.DataModel;

    /// <summary>
    /// Invnetory records
    /// </summary>
    [DynamoDBTable("vehicle_inventory")]
    public class InventoryItem
    {
        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>
        /// The client identifier.
        /// </value>
        [DynamoDBHashKey("clientIdentifier")]
        public string ClientIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the vin.
        /// </summary>
        /// <value>
        /// The vin.
        /// </value>
        [DynamoDBProperty("vin")]
        public string VIN { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        [DynamoDBProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the book value.
        /// </summary>
        /// <value>
        /// The book value.
        /// </value>
        [DynamoDBProperty("bookValue")]
        public string BookValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="InventoryItem"/> is certified.
        /// </summary>
        /// <value>
        ///   <c>true</c> if certified; otherwise, <c>false</c>.
        /// </value>
        [DynamoDBProperty("certified")]
        public string Certified { get; set; }

        /// <summary>
        /// Gets or sets the doors.
        /// </summary>
        /// <value>
        /// The doors.
        /// </value>
        [DynamoDBProperty("doors")]
        public int Doors { get; set; }

        /// <summary>
        /// Gets or sets the type of the drive.
        /// </summary>
        /// <value>
        /// The type of the drive.
        /// </value>
        [DynamoDBProperty("driveType")]
        public string DriveType { get; set; }
    }
}
