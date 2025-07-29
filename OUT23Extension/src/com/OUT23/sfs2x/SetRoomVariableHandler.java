package com.OUT23.sfs2x;

import com.smartfoxserver.v2.api.SFSApi;
import com.smartfoxserver.v2.entities.Room;
import com.smartfoxserver.v2.entities.User;
import com.smartfoxserver.v2.entities.variables.RoomVariable;
import com.smartfoxserver.v2.entities.variables.SFSRoomVariable;
import com.smartfoxserver.v2.entities.data.ISFSObject;
import com.smartfoxserver.v2.extensions.BaseClientRequestHandler;

import java.util.Arrays;
import java.util.List;

public class SetRoomVariableHandler extends BaseClientRequestHandler {

    @Override
    public void handleClientRequest(User user, ISFSObject params) {
        try {
            String name = params.getUtfString("name");
            boolean value = params.getBool("value");

            trace("🔧 Recibida petición para setRoomVariable: " + name + " = " + value);

            Room room = user.getLastJoinedRoom(); 

            RoomVariable rv = new SFSRoomVariable(name, value);
            rv.setGlobal(true);       
            rv.setPersistent(true);    
            rv.setHidden(false); 

            getApi().setRoomVariables(user, room, Arrays.asList(rv));

            trace("✅ Variable " + name + " actualizada correctamente a " + value);

            // LOG EXTRA: Imprimimos todas las variables enigm de la sala
            trace("📦 Estado actual de las variables 'enigmX' en la sala '" + room.getName() + "':");
            List<RoomVariable> roomVars = room.getVariables();
            for (RoomVariable var : roomVars) {
                if (var.getName().startsWith("enigm")) {
                    trace("   🔸 " + var.getName() + " = " + var.getValue() + " (Tipo: " + var.getType() + ")");
                }
            }

        } catch (Exception e) {
            trace("❌ Error en SetRoomVariableHandler: " + e.getMessage());
        }
    }
}
