﻿using System;
using System.Collections.Generic;
using System.Data.Linq.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Data.Linq
{
	internal static class Strings
	{
		public const string InsertCallbackComment = "--Callback into user code for insert.";
		public const string UpdateCallbackComment = "--Callback into user code for update.";
		public const string DeleteCallbackComment = "--Callback into user code for delete.";
		public const string DatabaseGeneratedAlreadyExistingKey = "The database generated a key that is already in use.";
		public const string RowNotFoundOrChanged = "Row not found or changed.";
		public const string CantAddAlreadyExistingKey = "Cannot add an entity with a key that is already in use.";
		public const string CannotAddChangeConflicts = "Cannot add change conflicts. They are added automatically during SubmitChanges.";
		public const string CannotRemoveChangeConflicts = "Cannot remove change conflicts. ";
		public const string InconsistentAssociationAndKeyChange = "The member '{0}' was changed to be inconsistent with the association member '{1}'.";
		public const string UnableToDetermineDataContext = "Unable to determine DataContext for compiled query execution.";
		public const string ArgumentTypeHasNoIdentityKey = "The type '{0}' has no identity key.";
		public const string CouldNotConvert = "Could not convert from type '{0}' to type '{1}'.";
		public const string CannotRemoveUnattachedEntity = "Cannot remove an entity that has not been attached.";
		public const string ColumnMappedMoreThanOnce = "Mapping Problem: The database column '{0}' is mapped more than once.";
		public const string CouldNotAttach = "Item already exists in data context.";
		public const string CouldNotGetTableForSubtype = "Could not retrieve a Table for inheritance subtype '{0}', try Table of {1} instead.";
		public const string CouldNotRemoveRelationshipBecauseOneSideCannotBeNull = "An attempt was made to remove a relationship between a {0} and a {1}. However, one of the relationship's foreign keys ({2}) cannot be set to null.";
		public const string EntitySetAlreadyLoaded = "The EntitySet is already loaded and the source cannot be changed.";
		public const string EntitySetModifiedDuringEnumeration = "EntitySet was modified during enumeration.";
		public const string ExpectedQueryableArgument = "Argument {0} does not implement {1}.";
		public const string ExpectedUpdateDeleteOrChange = "Expected update, delete, or change.";
		public const string KeyIsWrongSize = "Key is the wrong size. Expected {0}. Actual {1}.";
		public const string KeyValueIsWrongType = "Key value is the wrong type. Expected assignable from {0}. Actual {1}.";
		public const string IdentityChangeNotAllowed = "Value of member '{0}' of an object of type '{1}' changed.\r\nA member defining the identity of the object cannot be changed.\r\nConsider adding a new object with new identity and deleting the existing one instead.";
		public const string DbGeneratedChangeNotAllowed = "Value of member '{0}' of an object of type '{1}' changed.\r\nA member that is computed or generated by the database cannot be changed.";
		public const string ModifyDuringAddOrRemove = "Could not modify EntitySet.";
		public const string ProviderDoesNotImplementRequiredInterface = "Specified provider type '{0}' does not implement '{1}'.";
		public const string ProviderTypeNull = "Non-null provider type expected.";
		public const string TypeCouldNotBeAdded = "Instance of type '{0}' could not be added. This type is not part of the mapped type system.";
		public const string TypeCouldNotBeRemoved = "Instance of type '{0}' could not be removed. This type is not part of the mapped type system.";
		public const string TypeCouldNotBeTracked = "Instance of type '{0}' could not be tracked. This type is not part of the mapped type system.";
		public const string TypeIsNotEntity = "The type '{0}' is not an entity.";
		public const string UnrecognizedRefreshObject = "An object specified for refresh is not recognized.";
		public const string UnhandledExpressionType = "Unhandled Expression Type: {0}";
		public const string UnhandledBindingType = "Unhandled Binding Type: {0}";
		public const string ObjectTrackingRequired = "Object tracking is not enabled for the current data context instance.";
		public const string OptionsCannotBeModifiedAfterQuery = "Data context options cannot be modified after results have been returned from a query.";
		public const string DeferredLoadingRequiresObjectTracking = "Deferred loading requires that object tracking is also enabled.";
		public const string SubqueryDoesNotSupportOperator = "The operator '{0}' is not supported in Subquery.";
		public const string SubqueryNotSupportedOn = "Subquery is not supported on '{0}'.";
		public const string SubqueryNotSupportedOnType = "Subquery is not supported on '{0}' of type '{1}'.";
		public const string SubqueryNotAllowedAfterFreeze = "SetSubquery is not allowed after freeze or attach to DataContext.";
		public const string IncludeNotAllowedAfterFreeze = "LoadWith is not allowed after freeze or attach to DataContext.";
		public const string LoadOptionsChangeNotAllowedAfterQuery = "Setting load options is not allowed after results have been returned from a query.";
		public const string IncludeCycleNotAllowed = "Cycles not allowed in LoadOptions LoadWith type graph.";
		public const string SubqueryMustBeSequence = "Subquery must be a sequence expression.";
		public const string RefreshOfDeletedObject = "Unable to refresh the specified object.  The object no longer exists in the database.";
		public const string RefreshOfNewObject = "An object specified for refresh is pending insert and cannot be refreshed.";
		public const string CannotChangeInheritanceType = "Not allowed: Inheritance discriminator change from '{0}' to '{1}' would change type from '{2}' to '{3}'.";
		public const string DataContextCannotBeUsedAfterDispose = "DataContext accessed after Dispose.";
		public const string TypeIsNotMarkedAsTable = "The type '{0}' is not mapped as a Table.";
		public const string NonEntityAssociationMapping = "Invalid association mapping for member '{0}.{1}'.  '{2}' is not an entity.";
		public const string CannotPerformCUDOnReadOnlyTable = "Can't perform Create, Update, or Delete operations on '{0}' because it has no primary key.";
		public const string UpdatesFailedMessage = "{0} of {1} updates failed.";
		public const string CycleDetected = "A cycle was detected in the set of changes";
		public const string CantAddAlreadyExistingItem = "Cannot add an entity that already exists.";
		public const string InsertAutoSyncFailure = "Member AutoSync failure. For members to be AutoSynced after insert, the type must either have an auto-generated identity, or a key that is not modified by the database after insert.";
		public const string EntitySetDataBindingWithAbstractBaseClass = "Cannot add an instance of an abstract class to EntitySet<{0}>.";
		public const string EntitySetDataBindingWithNonPublicDefaultConstructor = "{0} must have a parameterless constructor when using IBindingList to add new instances.";
		public const string InvalidLoadOptionsLoadMemberSpecification = "The expression specified must be of the form p.A, where p is the parameter and A is a property or field member. ";
		public const string EntityIsTheWrongType = "The entity is not of the correct type.";
		public const string OriginalEntityIsWrongType = "The original state instance has the wrong type.";
		public const string CannotAttachAlreadyExistingEntity = "Cannot attach an entity that already exists.";
		public const string CannotAttachAsModifiedWithoutOriginalState = "An entity can only be attached as modified without original state if it declares a version member or does not have an update check policy.";
		public const string CannotPerformOperationDuringSubmitChanges = "The operation cannot be performed during a call to SubmitChanges.";
		public const string CannotPerformOperationOutsideSubmitChanges = "The operation can only be performed inside a user override method during a call to SubmitChanges.";
		public const string CannotPerformOperationForUntrackedObject = "The operation cannot be performed for the entity because it is not being change tracked.";
		public const string CannotAttachAddNonNewEntities = "An attempt has been made to Attach or Add an entity that is not new, perhaps having been loaded from another DataContext.  This is not supported.";
		public const string QueryWasCompiledForDifferentMappingSource = "Query was compiled for a different mapping source than the one associated with the specified DataContext.";

		public static string DefaultErrorMessage([CallerMemberName]string errorName = null)
			=> errorName;

		public static string DefaultErrorMessage(object[] args, [CallerMemberName]string errorName = null)
			=> $"{errorName}: {String.Join(", ", args)}.";
	}
}