﻿// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information. 

using Office365APIEditor.UI;
using Office365APIEditor.ViewerHelper;
using Office365APIEditor.ViewerHelper.Data;
using Office365APIEditor.ViewerHelper.Data.ToDoAPI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office365APIEditor
{
    public partial class FolderViewerForm : Form
    {
        FolderInfo targetFolder;
        string targetFolderDisplayName;

        string currentId = "";

        private bool isFormClosing = false;
        private ViewerRequestHelper viewerRequestHelper;

        public FolderViewerForm(FolderInfo TargetFolderInfo, string TargetFolderDisplayName)
        {
            InitializeComponent();
            
            targetFolder = TargetFolderInfo;
            targetFolderDisplayName = TargetFolderDisplayName;
        }

        private async void FolderViewerForm_Load(object sender, System.EventArgs e)
        {
            Icon = Properties.Resources.DefaultIcon;

            toolStripStatusLabel_Status.Text = "Loading all items...";

            string typeForWindowTitle = "";

            switch (targetFolder.Type)
            {
                case FolderContentType.Message:
                case FolderContentType.MsgFolderRoot:
                case FolderContentType.Drafts:
                    typeForWindowTitle = "Message items";
                    break;
                case FolderContentType.Contact:
                    typeForWindowTitle = "Contact items";
                    break;
                case FolderContentType.Calendar:
                    typeForWindowTitle = "Calendar items";
                    break;
                case FolderContentType.Task:
                    typeForWindowTitle = "To Do tasks";
                    break;
                default:
                    typeForWindowTitle = "items";
                    break;
            }

            Text = typeForWindowTitle + " in " + targetFolderDisplayName;

            viewerRequestHelper = new ViewerRequestHelper();

            switch (targetFolder.Type)
            {
                case FolderContentType.Message:
                    // Add columns.
                    PrepareMessageItemListColumns();

                    bool hasMessageItem = await viewerRequestHelper.HasActualMessageItemAsync(targetFolder.ID);
                    
                    if (hasMessageItem)
                    {
                        // Get items.
                        await LoadAllMessagesAsync();
                    }
                    else
                    {
                        // This folder seems to be a message folder but it does not contain message items.

                        var mailFolder = await viewerRequestHelper.GetMailFolderAsync(targetFolder.ID);
                        
                        if (mailFolder.TotalItemCount != 0 && MessageBox.Show("TotalItemCount of this folder is not 0 but getting items of this folder was failed.\r\nDo you want to retry getting items as Contact item?", "Office365APIEditor", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            // Retry as Contact item.

                            targetFolder.Type = FolderContentType.Contact;

                            Text = FolderContentType.Contact.ToString() + " items in " + targetFolderDisplayName;

                            // Reset DataGrid.
                            if (dataGridView_ItemList.InvokeRequired)
                            {
                                dataGridView_ItemList.Invoke(new MethodInvoker(delegate
                                {
                                    for (int i = dataGridView_ItemList.Columns.Count - 1; i >= 0; i--)
                                    {
                                        dataGridView_ItemList.Columns.RemoveAt(i);
                                    }
                                }));
                            }
                            else
                            {
                                for (int i = dataGridView_ItemList.Columns.Count - 1; i >= 0; i--)
                                {
                                    dataGridView_ItemList.Columns.RemoveAt(i);
                                }
                            }

                            // Add columns.
                            PrepareContactItemListColumns();

                            // Get items.
                            await LoadAllContactsAsync();
                        }
                    }

                    break;
                case FolderContentType.Contact:
                    // Add columns.
                    PrepareContactItemListColumns();

                    // Get items.
                    await LoadAllContactsAsync();

                    break;
                case FolderContentType.Calendar:
                    // Add columns.
                    PrepareCalendarItemListColumns();

                    // Get items.
                    await LoadAllEventsAsync();

                    break;
                case FolderContentType.MsgFolderRoot:
                case FolderContentType.Drafts:
                    // Add columns.
                    PrepareMessageItemListColumns();

                    // Get items.
                    await LoadAllMessagesAsync();

                    break;
                case FolderContentType.Task:
                    // Add Columns.
                    PrepareToDoTaskListColumns();

                    // Get items.
                    await LoadAllToDoTasksAsync();

                    break;
                case FolderContentType.DummyCalendarGroupRoot:
                case FolderContentType.DummyTaskGroupRoot:
                case FolderContentType.TaskGroup:
                default:
                    break;
            }

            toolStripStatusLabel_Status.Text = "Loaded all items.";
        }

        private void FolderViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Change the flag to avoid unnecessary error message.
            isFormClosing = true;
        }

        private void PrepareMessageItemListColumns()
        {
            if (dataGridView_ItemList.InvokeRequired)
            {
                dataGridView_ItemList.Invoke(new MethodInvoker(delegate
                {
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Subject", HeaderText = "Subject", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Sender", HeaderText = "Sender", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ToRecipients", HeaderText = "ToRecipients", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ReceivedDateTime", HeaderText = "ReceivedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "SentDateTime", HeaderText = "SentDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                }));
            }
            else
            {
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Subject", HeaderText = "Subject", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Sender", HeaderText = "Sender", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ToRecipients", HeaderText = "ToRecipients", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "ReceivedDateTime", HeaderText = "ReceivedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "SentDateTime", HeaderText = "SentDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
            }
        }

        private void PrepareContactItemListColumns()
        {
            if (dataGridView_ItemList.InvokeRequired)
            {
                dataGridView_ItemList.Invoke(new MethodInvoker(delegate
                {
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DisplayName", HeaderText = "DisplayName", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                }));
            }
            else
            {
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "DisplayName", HeaderText = "DisplayName", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
            }
        }

        private void PrepareCalendarItemListColumns()
        {
            if (dataGridView_ItemList.InvokeRequired)
            {
                dataGridView_ItemList.Invoke(new MethodInvoker(delegate
                {
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Subject", HeaderText = "Subject", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Organizer", HeaderText = "Organizer", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Attendees", HeaderText = "Attendees", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Start", HeaderText = "Start (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 120 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "End", HeaderText = "End (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 120 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "IsAllDay", HeaderText = "IsAllDay", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 60 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                }));
            }
            else
            {
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Subject", HeaderText = "Subject", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Organizer", HeaderText = "Organizer", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Attendees", HeaderText = "Attendees", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Start", HeaderText = "Start (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 120 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "End", HeaderText = "End (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 120 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "IsAllDay", HeaderText = "IsAllDay", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 60 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
            }
        }

        [Obsolete]
        private void PrepareTaskItemListColumns()
        {
            if (dataGridView_ItemList.InvokeRequired)
            {
                dataGridView_ItemList.Invoke(new MethodInvoker(delegate
                {
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Subject", HeaderText = "Subject", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "HasAttachments", HeaderText = "HasAttachments", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 60 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "LastModifiedDateTime", HeaderText = "LastModifiedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 180 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Status", HeaderText = "Status", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                }));
            }
            else
            {
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Subject", HeaderText = "Subject", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "HasAttachments", HeaderText = "HasAttachments", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 60 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "LastModifiedDateTime", HeaderText = "LastModifiedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 180 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Status", HeaderText = "Status", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
            }
        }

        private void PrepareToDoTaskListColumns()
        {
            if (dataGridView_ItemList.InvokeRequired)
            {
                dataGridView_ItemList.Invoke(new MethodInvoker(delegate
                {
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Title", HeaderText = "Title", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "LastModifiedDateTime", HeaderText = "LastModifiedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 180 });
                    dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Status", HeaderText = "Status", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                }));
            }
            else
            {
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Title", HeaderText = "Title", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "CreatedDateTime", HeaderText = "CreatedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 160 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "LastModifiedDateTime", HeaderText = "LastModifiedDateTime (UTC)", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 180 });
                dataGridView_ItemList.Columns.Add(new DataGridViewTextBoxColumn() { Name = "Status", HeaderText = "Status", Resizable = DataGridViewTriState.True, AutoSizeMode = DataGridViewAutoSizeColumnMode.NotSet, Frozen = false, MinimumWidth = 100 });
            }
        }

        private async Task<bool> LoadAllMessagesAsync()
        {
            var messages = await viewerRequestHelper.GetAllMessagesFirstPageAsync(targetFolder.ID);

            do
            {
                ShowMessages(messages.CurrentPage);

                if (!messages.MorePage)
                {
                    break;
                }

                messages = await viewerRequestHelper.GetAllMessagesPageAsync(messages.NextLink);
            } while (messages.CurrentPage.Count > 0);

            return true;
        }

        private async Task<bool> LoadAllContactsAsync()
        {
            var contacts = await viewerRequestHelper.GetAllContactsFirstPageAsync(targetFolder.ID);

            do
            {
                ShowContacts(contacts.CurrentPage);

                if (!contacts.MorePage)
                {
                    break;             
                }

                contacts = await viewerRequestHelper.GetAllContactsPageAsync(contacts.NextLink);
            } while (contacts.CurrentPage.Count > 0);

            return true;
        }

        private async Task<bool> LoadAllEventsAsync()
        {
            var events = await viewerRequestHelper.GetAllEventsFirstPageAsync(targetFolder.ID);

            do
            {
                ShowEvents(events.CurrentPage);

                if (!events.MorePage)
                {
                    break;
                }

                events = await viewerRequestHelper.GetAllEventsPageAsync(events.NextLink);
            } while (events.CurrentPage.Count > 0);

            return true;
        }

        [Obsolete]
        private async Task<bool> LoadAllTasksAsync()
        {
            var tasks = await viewerRequestHelper.GetAllTasksFirstPageAsync(targetFolder.ID);

            do
            {
                ShowTasks(tasks.CurrentPage);

                if (!tasks.MorePage)
                {
                    break;
                }

                tasks = await viewerRequestHelper.GetAllTasksPageAsync(tasks.NextLink);
            } while (tasks.CurrentPage.Count > 0);

            return true;
        }

        private async Task<bool> LoadAllToDoTasksAsync()
        {
            var toDoTasks = await viewerRequestHelper.GetAllToDoTasksFirstPageAsync(targetFolder.ID);

            do
            {
                //ShowTasks(toDoTasks.CurrentPage);
                ShowToDoTasks(toDoTasks.CurrentPage);

                if (!toDoTasks.MorePage)
                {
                    break;
                }

                toDoTasks = await viewerRequestHelper.GetAllToDoTasksPageAsync(toDoTasks.NextLink);
            } while (toDoTasks.CurrentPage.Count > 0);

            return true;
        }

        private void ShowMessages(List<ViewerHelper.Data.MailAPI.Message> messages)
        {
            // Show all messages in List.

            try
            {
                foreach (var item in messages)
                {
                    // Add new row.
                    string rowReceivedDateTime = item.ReceivedDateTime ?? "";
                    string rowCreatedDateTime = item.CreatedDateTime ?? "";
                    string rowSentDateTime = item.SentDateTime ?? "";
                    string subject = item.Subject ?? "";
                    string sender = (item.Sender != null && item.Sender.EmailAddress != null && item.Sender.EmailAddress.Address != null) ? item.Sender.EmailAddress.Address : "";
                    string recipients = (item.ToRecipients != null) ? ConvertRecipientsListToString(item.ToRecipients) : "";
                    string isDraft = (item.IsDraft != null && item.IsDraft.HasValue) ? item.IsDraft.Value.ToString() : "";

                    if (DateTime.TryParse(rowReceivedDateTime, out DateTime receivedDateTime) == false)
                    {
                        receivedDateTime = DateTime.MinValue;
                    }

                    if (DateTime.TryParse(rowCreatedDateTime, out DateTime createdDateTime) == false)
                    {
                        createdDateTime = DateTime.MinValue;
                    }

                    if (DateTime.TryParse(rowSentDateTime, out DateTime sentDateTime) == false)
                    {
                        sentDateTime = DateTime.MinValue;
                    }

                    DataGridViewRow itemRow = new DataGridViewRow
                    {
                        Tag = item.Id
                    };
                    itemRow.CreateCells(dataGridView_ItemList, new object[] { subject, sender, recipients, receivedDateTime, createdDateTime, sentDateTime });

                    if (item.IsDraft != null && item.IsDraft.HasValue && item.IsDraft.Value == true)
                    {
                        // This item is draft.
                        itemRow.ContextMenuStrip = contextMenuStrip_ItemList_DraftItem;
                    }
                    else
                    {
                        // This item is not draft.
                        itemRow.ContextMenuStrip = contextMenuStrip_ItemList;
                    }

                    if (dataGridView_ItemList.InvokeRequired)
                    {
                        dataGridView_ItemList.Invoke(new MethodInvoker(delegate { dataGridView_ItemList.Rows.Add(itemRow); }));
                    }
                    else
                    {
                        dataGridView_ItemList.Rows.Add(itemRow);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                if (isFormClosing)
                {
                    // It seems that this window was closed.
                    // Do nothing.
                }
                else
                {
                    MessageBox.Show(ex.Message, "Office365APIEditor");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().FullName + "\r\n" + ex.Message, "Office365APIEditor");
            }
        }

        private void ShowContacts(List<ViewerHelper.Data.ContactsAPI.Contact> contacts)
        {
            // Show all contacts in List.

            try
            {
                foreach (var item in contacts)
                {
                    // Add new row.

                    string displayName = item.DisplayName ?? "";
                    string rowCreatedDateTime = item.CreatedDateTime ?? "";

                    if (DateTime.TryParse(rowCreatedDateTime, out DateTime createdDateTime) == false)
                    {
                        createdDateTime = DateTime.MinValue;
                    }

                    DataGridViewRow itemRow = new DataGridViewRow
                    {
                        Tag = item.Id
                    };
                    itemRow.CreateCells(dataGridView_ItemList, new object[] { displayName, createdDateTime });
                    itemRow.ContextMenuStrip = contextMenuStrip_ItemList;

                    if (dataGridView_ItemList.InvokeRequired)
                    {
                        dataGridView_ItemList.Invoke(new MethodInvoker(delegate { dataGridView_ItemList.Rows.Add(itemRow); }));
                    }
                    else
                    {
                        dataGridView_ItemList.Rows.Add(itemRow);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                if (isFormClosing)
                {
                    // It seems that this window was closed.
                    // Do nothing.
                }
                else
                {
                    MessageBox.Show(ex.Message, "Office365APIEditor");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().FullName + "\r\n" + ex.Message, "Office365APIEditor");
            }
        }

        private void ShowEvents(List<ViewerHelper.Data.CalendarAPI.Event> events)
        {
            // Show all events in List.

            try
            {
                foreach (var item in events)
                {
                    // Add new row.

                    string subject = item.Subject ?? "";
                    string organizer = (item.Organizer != null && item.Organizer.EmailAddress != null && item.Organizer.EmailAddress.Address != null) ? item.Organizer.EmailAddress.Address : "";
                    string attendees = (item.Attendees != null) ? ConvertAttendeesListToString(item.Attendees) : "";
                    DateTime start = (item.Start != null) ? item.Start.ToUniversalTime() : DateTime.MinValue;
                    DateTime end = (item.End != null) ? item.End.ToUniversalTime() : DateTime.MinValue;
                    string isAllDay = (item.IsAllDay != null) ? item.IsAllDay.ToString() : "";
                    string rowCreatedDateTime = item.CreatedDateTime ?? "";

                    if (DateTime.TryParse(rowCreatedDateTime, out DateTime createdDateTime) == false)
                    {
                        createdDateTime = DateTime.MinValue;
                    }

                    DataGridViewRow itemRow = new DataGridViewRow
                    {
                        Tag = item.Id
                    };
                    itemRow.CreateCells(dataGridView_ItemList, new object[] { subject, organizer, attendees, start, end, isAllDay, createdDateTime });
                    itemRow.ContextMenuStrip = contextMenuStrip_ItemList;

                    if (dataGridView_ItemList.InvokeRequired)
                    {
                        dataGridView_ItemList.Invoke(new MethodInvoker(delegate { dataGridView_ItemList.Rows.Add(itemRow); }));
                    }
                    else
                    {
                        dataGridView_ItemList.Rows.Add(itemRow);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                if (isFormClosing)
                {
                    // It seems that this window was closed.
                    // Do nothing.
                }
                else
                {
                    MessageBox.Show(ex.Message, "Office365APIEditor");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().FullName + "\r\n" + ex.Message, "Office365APIEditor");
            }
        }

        [Obsolete]
        private void ShowTasks(List<ViewerHelper.Data.TaskAPI.Task> tasks)
        {
            // Show all tasks in List.

            try
            {
                foreach (var item in tasks)
                {
                    // Add new row.
                    string subject = item.Subject ?? "";
                    string hasAttachments = (item.HasAttachments != null) ? item.HasAttachments.Value.ToString() : "";
                    string rowCreatedDateTime = item.CreatedDateTime ?? "";
                    string rowLastModifiedDateTime = item.LastModifiedDateTime ?? "";
                    string status = item.Status ?? "";

                    if (DateTime.TryParse(rowCreatedDateTime, out DateTime createdDateTime) == false)
                    {
                        createdDateTime = DateTime.MinValue;
                    }

                    if (DateTime.TryParse(rowLastModifiedDateTime, out DateTime lastModifiedDateTime) == false)
                    {
                        lastModifiedDateTime = DateTime.MinValue;
                    }

                    DataGridViewRow itemRow = new DataGridViewRow
                    {
                        Tag = item.Id
                    };
                    itemRow.CreateCells(dataGridView_ItemList, new object[] { subject, hasAttachments, createdDateTime, lastModifiedDateTime, status });
                    itemRow.ContextMenuStrip = contextMenuStrip_ItemList;

                    if (dataGridView_ItemList.InvokeRequired)
                    {
                        dataGridView_ItemList.Invoke(new MethodInvoker(delegate { dataGridView_ItemList.Rows.Add(itemRow); }));
                    }
                    else
                    {
                        dataGridView_ItemList.Rows.Add(itemRow);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                if (isFormClosing)
                {
                    // It seems that this window was closed.
                    // Do nothing.
                }
                else
                {
                    MessageBox.Show(ex.Message, "Office365APIEditor");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().FullName + "\r\n" + ex.Message, "Office365APIEditor");
            }
        }

        private void ShowToDoTasks(List<ToDoTask> toDoTasks)
        {
            // Show all ToDoTasks in List.

            try
            {
                foreach (var item in toDoTasks)
                {
                    // Add new row.
                    string subject = item.Title ?? "";
                    string rowCreatedDateTime = item.CreatedDateTime ?? "";
                    string rowLastModifiedDateTime = item.LastModifiedDateTime ?? "";
                    string status = item.Status ?? "";

                    if (DateTime.TryParse(rowCreatedDateTime, out DateTime createdDateTime) == false)
                    {
                        createdDateTime = DateTime.MinValue;
                    }

                    if (DateTime.TryParse(rowLastModifiedDateTime, out DateTime lastModifiedDateTime) == false)
                    {
                        lastModifiedDateTime = DateTime.MinValue;
                    }

                    DataGridViewRow itemRow = new DataGridViewRow
                    {
                        Tag = item.Id
                    };
                    itemRow.CreateCells(dataGridView_ItemList, new object[] { subject, createdDateTime, lastModifiedDateTime, status });

                    // Currently, ToDoTask does not support attachment.
                    // TODO: Add new ContextMenuStrip to view linkedResources.
                    itemRow.ContextMenuStrip = null;

                    if (dataGridView_ItemList.InvokeRequired)
                    {
                        dataGridView_ItemList.Invoke(new MethodInvoker(delegate { dataGridView_ItemList.Rows.Add(itemRow); }));
                    }
                    else
                    {
                        dataGridView_ItemList.Rows.Add(itemRow);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                if (isFormClosing)
                {
                    // It seems that this window was closed.
                    // Do nothing.
                }
                else
                {
                    MessageBox.Show(ex.Message, "Office365APIEditor");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().FullName + "\r\n" + ex.Message, "Office365APIEditor");
            }
        }

        private string ConvertRecipientsListToString(IList<Recipient> RecipientsList)
        {
            StringBuilder result = new StringBuilder();

            foreach (Recipient address in RecipientsList)
            {
                result.Append(address.EmailAddress.Address + "; ");
            }

            return result.ToString().Trim(' ', ';');
        }

        private string ConvertAttendeesListToString(IList<Attendee> AttendeesList)
        {
            StringBuilder result = new StringBuilder();

            foreach (Attendee address in AttendeesList)
            {
                result.Append(address.EmailAddress.Address + "; ");
            }

            return result.ToString().Trim(' ', ';');
        }

        private void dataGridView_ItemList_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                // A header was double clicked.
                return;
            }

            // Select the row for the context menu.
            dataGridView_ItemList.Rows[e.RowIndex].Selected = true;
        }

        private async void dataGridView_ItemList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                // A header was double clicked.
                return;
            }

            // Get the item ID of clicked row.
            string id = dataGridView_ItemList.Rows[e.RowIndex].Tag.ToString();

            if (currentId == id)
            {
                return;
            }
            else
            {
                currentId = id;
            }

            // Reset rows.
            dataGridView_ItemProps.Rows.Clear();
            foreach (DataGridViewColumn col in dataGridView_ItemProps.Columns)
            {
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            switch (targetFolder.Type)
            {
                case FolderContentType.Message:
                case FolderContentType.MsgFolderRoot:
                case FolderContentType.Drafts:
                    CreatePropTable(await viewerRequestHelper.GetMessageAsync(id));
                    break;
                case FolderContentType.Contact:
                    CreatePropTable(await viewerRequestHelper.GetContactAsync(id));
                    break;
                case FolderContentType.Calendar:
                    CreatePropTable(await viewerRequestHelper.GetEventAsync(id));
                    break;
                case FolderContentType.Task:
                    CreatePropTable(await viewerRequestHelper.GetToDoTaskAsync(targetFolder.ID, id));
                    break;
                case FolderContentType.DummyCalendarGroupRoot:
                case FolderContentType.DummyTaskGroupRoot:
                case FolderContentType.TaskGroup:
                    break;
                default:
                    break;
            }
        }

        private void CreatePropTable(OutlookRestApiBaseObject OutlookItem)
        {
            var properties = OutlookItem.GetRawProperties();            

            try
            {
                foreach (KeyValuePair<string, string> item in properties)
                {
                    DataGridViewRow propRow = new DataGridViewRow();

                    string valueString = (item.Value == null) ? "" : item.Value.ToString();

                    propRow.CreateCells(dataGridView_ItemProps, new object[] { item.Key, valueString, "Dynamic" });

                    if (dataGridView_ItemProps.InvokeRequired)
                    {
                        dataGridView_ItemProps.Invoke(new MethodInvoker(delegate
                        {
                            dataGridView_ItemProps.Rows.Add(propRow);
                        }));
                    }
                    else
                    {
                        dataGridView_ItemProps.Rows.Add(propRow);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Office365APIEditor");
            }
        }

        private void dataGridView_ItemList_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Do nothing.
        }

        private void contextMenuStrip_ItemList_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = (dataGridView_ItemList.SelectedRows.Count == 0);
        }

        private void ToolStripMenuItem_DisplayAttachments_Click(object sender, EventArgs e)
        {
            DisplayAttachments();
        }

        private void ToolStripMenuItem_DisplayAttachments_DraftItem_Click(object sender, EventArgs e)
        {
            DisplayAttachments();
        }

        private void DisplayAttachments()
        {
            if (dataGridView_ItemList.SelectedRows.Count == 0)
            {
                return;
            }

            AttachmentViewerForm attachmentViewer = new AttachmentViewerForm(targetFolder, dataGridView_ItemList.SelectedRows[0].Tag.ToString(), dataGridView_ItemList.SelectedRows[0].Cells[0].Value.ToString());
            attachmentViewer.Show();
        }

        private void dataGridView_ItemProps_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                // A header was double clicked.
                return;
            }

            // Get the value of double-clicked row.
            object name = dataGridView_ItemProps.Rows[e.RowIndex].Cells[0].Value;
            string nameString = (name == null) ? "" : name.ToString();

            object value = dataGridView_ItemProps.Rows[e.RowIndex].Cells[1].Value;
            string valueString = (value == null) ? "" : value.ToString();


            PropertyViewerForm propertyViewer = new PropertyViewerForm(nameString, valueString)
            {
                Owner = this
            };
            propertyViewer.Show();
        }

        private void ToolStripMenuItem_Edit_DraftItem_Click(object sender, EventArgs e)
        {
            if (dataGridView_ItemList.SelectedRows.Count == 0)
            {
                return;
            }

            SendMailForm sendMailForm = new SendMailForm(dataGridView_ItemList.SelectedRows[0].Tag.ToString());
            sendMailForm.Show();
        }
    }
}