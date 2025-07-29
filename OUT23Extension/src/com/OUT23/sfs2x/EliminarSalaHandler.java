package com.OUT23.sfs2x;

import com.smartfoxserver.v2.entities.Room;
import com.smartfoxserver.v2.entities.User;
import com.smartfoxserver.v2.entities.data.ISFSObject;
import com.smartfoxserver.v2.entities.data.SFSObject;
import com.smartfoxserver.v2.extensions.BaseClientRequestHandler;

public class EliminarSalaHandler extends BaseClientRequestHandler {

    @Override
    public void handleClientRequest(User user, ISFSObject params) {
        String codigo = params.getUtfString("codigo");

        Room sala = getParentExtension().getParentZone().getRoomByName(codigo);

        SFSObject response = new SFSObject();

        if (sala != null) {
            getApi().removeRoom(sala);
            response.putBool("ok", true); 
        } else {
            response.putUtfString("error", "Sala no encontrada");
        }

        send("eliminar_sala", response, user); 
    }
}
