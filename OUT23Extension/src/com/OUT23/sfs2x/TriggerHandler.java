package com.OUT23.sfs2x;

import com.smartfoxserver.v2.extensions.BaseClientRequestHandler;
import com.smartfoxserver.v2.entities.User;
import com.smartfoxserver.v2.entities.Room;
import com.smartfoxserver.v2.entities.variables.RoomVariable;
import com.smartfoxserver.v2.entities.variables.SFSRoomVariable;
import com.smartfoxserver.v2.entities.data.*;
import com.smartfoxserver.v2.exceptions.SFSVariableException;
import java.util.Arrays;

public class TriggerHandler extends BaseClientRequestHandler {

    @Override
    public void handleClientRequest(User sender, ISFSObject params) {
        Room room = sender.getLastJoinedRoom();

        if (!params.containsKey("object")) {
            trace("TriggerHandler: Missing 'object'.");
            return;
        }

        ISFSObject objData = params.getSFSObject("object");

        if (objData == null) {
            trace("TriggerHandler: Null object received.");
            return;
        }

        String objName = objData.getUtfString("name");

        if (objName == null || objName.isEmpty()) {
            trace("TriggerHandler: Invalid object name.");
            return;
        }

        // Crear o actualizar la RoomVariable correctamente
        RoomVariable newVar = new SFSRoomVariable(objName, objData);
        newVar.setGlobal(true);
        newVar.setPersistent(true);

        // Este método propaga la actualización a todos los clientes de la sala
		getApi().setRoomVariables(null, room, Arrays.asList(newVar));
		trace("Updated or created variable (and notified clients): " + objName);
    }
}
