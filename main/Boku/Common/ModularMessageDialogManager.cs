// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Boku.Common
{
    //dialog manager to attempt to make it easier to code in new dialogs throughout the code base
    // - dialogs will update/render in order added
    //
    //Note: if performance really warrants, consider using a pool for these - but dialogs should be an edge case
    // and there should rarely be more than one on screen - having a list may already be over kill
    class ModularMessageDialogManager
    {
        private static ModularMessageDialogManager m_instance;

        //list of active dialogs
        private List<ModularMessageDialog> m_dialogs;
        

        //accessors
        public static ModularMessageDialogManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new ModularMessageDialogManager();
                }
                return m_instance;
            }
        }

        //singleton constructor
        private ModularMessageDialogManager()
        {
            m_dialogs = new List<ModularMessageDialog>();
        }

        public bool IsDialogActive()
        {
            return m_dialogs.Count > 0;
        }

        //call once a frame to update the dialogs
        public void Update()
        {
            //first, update all dialogs
            for (int i = 0; i < m_dialogs.Count; ++i)
            {
                m_dialogs[i].Update();
            }

            //now, check if the active dialog needs to change
            if (m_dialogs.Count > 0)
            {
                //check if the active dialog has been dismissed
                if (!m_dialogs[0].Active)
                {
                    m_dialogs.RemoveAt(0);
                    
                    //are there more dialogs?  if so, activate the next
                    if (m_dialogs.Count > 0)
                    {
                        m_dialogs[0].Activate();
                    }
                }
            }
        }

        //render the active dialogs
        public void Render()
        {
            int numDialogs = m_dialogs.Count;
            for (int i = 0; i < numDialogs; ++i)
            {
                m_dialogs[i].Render();
            }
        }

        public void AddDialog(string text,
            ModularMessageDialog.ButtonHandler handlerA, string labelA)
        {
            AddDialog(text, handlerA, labelA,
                            null, null,
                            null, null,
                            null, null);
        }

        public void AddDialog(string text,
            ModularMessageDialog.ButtonHandler handlerA, string labelA,
            ModularMessageDialog.ButtonHandler handlerB, string labelB)
        {
            AddDialog(text, handlerA, labelA,
                            handlerB, labelB,
                            null, null,
                            null, null);
        }


        public void AddDialog(string text,
            ModularMessageDialog.ButtonHandler handlerA, string labelA,
            ModularMessageDialog.ButtonHandler handlerB, string labelB,
            ModularMessageDialog.ButtonHandler handlerX, string labelX,
            ModularMessageDialog.ButtonHandler handlerY, string labelY)
        {
            ModularMessageDialog newDialog = new ModularMessageDialog(text,
                                                                        handlerA, labelA,
                                                                        handlerB, labelB,
                                                                        handlerX, labelX,
                                                                        handlerY, labelY);

            if (m_dialogs.Count <= 0)
            {
                //only activate if no other dialogs are active, otherwise, add it to the list to wait its turn
                newDialog.Activate();
            }

            //add it to the list
            m_dialogs.Add(newDialog);
        }
    }
}
