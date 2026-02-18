using EFT.Communications;

namespace DES.Methods;

public class Methods
{
	public static void Notification(string message, ENotificationIconType notificationType = ENotificationIconType.Quest)
	{
		var msg = new GClass2314(message, ENotificationDurationType.Long, notificationType);
		NotificationManagerClass.DisplayNotification(msg);
	}
}