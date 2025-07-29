package com.OUT23.sfs2x;

import com.smartfoxserver.v2.extensions.BaseClientRequestHandler;
import com.smartfoxserver.v2.entities.Room;
import com.smartfoxserver.v2.entities.User;
import com.smartfoxserver.v2.entities.Zone;
import com.smartfoxserver.v2.entities.data.ISFSObject;
import com.smartfoxserver.v2.entities.data.SFSArray;
import com.smartfoxserver.v2.entities.data.SFSObject;

public class ObtenerSalasHandler extends BaseClientRequestHandler {

	@Override
	public void handleClientRequest(User sender, ISFSObject params) {
	    SFSArray array = new SFSArray();
	    Zone zone = getParentExtension().getParentZone();

	    for (Room room : zone.getRoomList()) {
	        if (!room.isHidden()) {
	            SFSObject obj = new SFSObject();
	            obj.putUtfString("type", room.getVariable("tipo") != null ? room.getVariable("tipo").getStringValue() : "N/A");
	            obj.putUtfString("codigo", room.getVariable("codigo") != null ? room.getVariable("codigo").getStringValue() : "N/A");
	            obj.putUtfString("nombre", room.getVariable("nombre") != null ? room.getVariable("nombre").getStringValue() : "N/A");
	            obj.putInt("maxusers", room.getMaxUsers());
	            array.addSFSObject(obj);
	        }
	    }

	    SFSObject response = new SFSObject();
	    response.putSFSArray("salas", array);
	    send("lista_salas", response, sender);
	}
}
