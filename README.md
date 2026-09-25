# WhatsApp Clone, Assignment 5

This UWP classroom project implements the six tasks in the supplied `Assignment_5.pdf`. It is a local chat simulation, with no WhatsApp connection and no email delivery.

## Open and run

1. Open `WhatsAppClone.slnx` in Visual Studio 2026 on Windows.
2. Install the Universal Windows Platform development workload and Windows SDK 10.0.26100.0 if Visual Studio requests them. Restore NuGet packages.
3. Select `Debug | x64` or `Debug | x86` and run on Local Machine.
4. Select **Create account**, register an email, phone number, and password, then log in with the email or phone number.

## Assignment features

1. Sign-up writes salted password hashes and account details to `users.json` in the application's local data folder. Login reads that file and reports a missing or unreadable file.
2. Forgot Password displays a simulated six-digit code for a registered email and pauses the send button for 30 seconds. The code is displayed in the app, not emailed, and does not reset the password.
3. Add Contact shows a modal form for a name, HTTP(S) profile image URL, and status. Valid contacts appear in the contact list.
4. Sending text shows the selected contact typing and adds an automatic reply two seconds later. The emoji menu inserts an emoji at the caret.
5. Attach Media opens the UWP image picker and shows the selected image in the active chat.
6. Clear Chat clears only the active conversation. The light/dark theme switch saves the selection to local settings.

Chats and manually added contacts remain in memory for this assignment and reset when the app closes. `users.json` remains on the local device. This local authentication is suitable for a coursework demo, not a production messaging service.
