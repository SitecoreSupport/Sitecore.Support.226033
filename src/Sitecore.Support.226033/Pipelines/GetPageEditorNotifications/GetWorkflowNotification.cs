using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor;
using Sitecore.ExperienceEditor.Switchers;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.Pipelines.GetPageEditorNotifications;
using Sitecore.SecurityModel;
using Sitecore.Shell.Applications.ContentManager.Panels;
using Sitecore.Shell.Framework.CommandBuilders;
using Sitecore.Workflows;

namespace Sitecore.Support.Pipelines.GetPageEditorNotifications
{
    public class GetWorkflowNotification : GetPageEditorNotificationsProcessor
    {
        public override void Process(GetPageEditorNotificationsArgs arguments)
        {
            Assert.ArgumentNotNull(arguments, "arguments");
            if (!WebUtility.IsEditAllVersionsTicked())
            {
                Item contextItem = arguments.ContextItem;
                if (contextItem != null)
                {
                    if (!contextItem.Access.CanReadLanguage())
                    {
                        return;
                    }
                    if (!contextItem.Access.CanWriteLanguage())
                    {
                        return;
                    }
                }
                using (new SecurityDisabler())
                {
                    Database database = (contextItem != null) ? contextItem.Database : null;
                    IWorkflowProvider obj = (database != null) ? database.WorkflowProvider : null;
                    IWorkflow workflow = (obj != null) ? obj.GetWorkflow(contextItem) : null;
                    WorkflowState workflowState = (workflow != null) ? workflow.GetState(contextItem) : null;
                    if (workflowState != null)
                    {
                        using (new LanguageSwitcher(WebUtility.ClientLanguage))
                        {
                            string description = GetWorkflowNotification.GetDescription(workflow, workflowState, database);
                            string icon = workflowState.Icon;
                            PageEditorNotification pageEditorNotification = new PageEditorNotification(description, PageEditorNotificationType.Information)
                            {
                                Icon = icon
                            };
                            WorkflowCommand[] array = default(WorkflowCommand[]);
                            bool canShowCommands = false;
                            using (new SecurityEnabler())
                            {
                                using (new ClientDatabaseSwitcher(database))
                                {
                                    array = WorkflowFilterer.FilterVisibleCommands(workflow.GetCommands(contextItem), contextItem);
                                    canShowCommands = this.CanShowCommands(contextItem, array);
                                }
                            }
                            if (canShowCommands)
                            {
                                WorkflowCommand[] array2 = array;
                                foreach (WorkflowCommand workflowCommand in array2)
                                {
                                    string displayName = workflowCommand.DisplayName;
                                    string text = new WorkflowCommandBuilder(contextItem, workflow, workflowCommand).ToString();
                                    if (Settings.WebEdit.AffectWorkflowForDatasourceItems)
                                    {
                                        text = text.Replace("item:workflow(", "webedit:workflowwithdatasourceitems(");
                                    }
                                    PageEditorNotificationOption item = new PageEditorNotificationOption(displayName, text);
                                    pageEditorNotification.Options.Add(item);
                                }
                            }
                            arguments.Notifications.Add(pageEditorNotification);
                        }
                    }
                }
            }
        }


      private bool CanShowCommands(Item item, WorkflowCommand[] commands)
      {
        Assert.ArgumentNotNull(item, "item");
        if (item.Appearance.ReadOnly || commands == null || commands.Length == 0 || !item.Access.CanWrite())
        {
          return false;
        }
        if (Context.IsAdministrator)
        {
          return true;
        }
        if (item.Access.CanWriteLanguage() && (item.Locking.CanLock() || item.Locking.HasLock()))
        {
          return true;
        }
        return false;
      }

        private static string GetDescription(IWorkflow workflow, WorkflowState state, Database database)
        {
            return WorkflowUtility.GetWorkflowStateDescription(workflow, state, database);
        }
    }
}