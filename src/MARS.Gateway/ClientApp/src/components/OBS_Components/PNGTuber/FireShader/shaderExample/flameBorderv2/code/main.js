let flame_height_controller = {
  height: 0,
  speed: 0,
  is_need_update: false,
};

function setup_flame_shader(
  shader,
  projection_matrix,
  noise_texture_size,
  settings
) {
  shader.set_uniform_float("time_effect", settings.time_effect);
  shader.set_uniform_float("sigma", settings.sigma);
  shader.set_uniform_float("flame_height", settings.flame_height);
  shader.set_uniform_float("luma_min", settings.luma_min);
  shader.set_uniform_float("luma_min_smooth", settings.luma_min_smooth);
  shader.set_uniform_int("is_mirror", settings.is_mirror);
  shader.set_uniform_int("is_reflected", settings.is_reflected);
  shader.set_uniform_int(
    "is_apply_to_alpha_layer",
    settings.is_apply_to_alpha_layer
  );
  shader.set_uniform_float("alpha_percentage", settings.alpha_percentage);

  let flame_multi_color = [
    settings.flame_multi_color.r / 255,
    settings.flame_multi_color.g / 255,
    settings.flame_multi_color.b / 255,
  ];

  shader.set_uniform_float("flame_multi_color", flame_multi_color);
  shader.set_uniform_matrix("projection_matrix", projection_matrix);
  shader.set_uniform_int("noise_texture", 0);
  shader.set_uniform_float("noise_texture_size", noise_texture_size);
}

//*****************************************************************************

function setup_texture_shader(
  shader,
  projection_matrix,
  resolution_size,
  textures_size
) {
  shader.set_uniform_float("iResolution", resolution_size);
  shader.set_uniform_float("texture_size", textures_size);
  shader.set_uniform_int("border_texture", 0);
  shader.set_uniform_int("corner_texture", 1);
  shader.set_uniform_matrix("projection_matrix", projection_matrix);
}

//*****************************************************************************

function update_flame_height(dt, shader_settings, height) {
  const min_height = Math.max(0.01, shader_settings.flame_min_height);
  const max_height = shader_settings.flame_max_height;

  const speed = flame_height_controller.speed;
  let next_height = flame_height_controller.height;

  next_height = clamp(next_height, min_height, max_height);

  let diff = Math.abs(next_height - height);
  let koeff = diff / (max_height - min_height);

  if (shader_settings.is_linear_flame_animation) {
    if (next_height > height) {
      height += dt * speed * koeff;
    } else if (next_height < height) {
      height -= dt * speed * koeff;
    }
  } else {
    height = next_height;
  }

  height = clamp(height, min_height, max_height);

  return height;
}

//*****************************************************************************

function push_vertices(out_vertices, out_tex_coords, object) {
  const model = object.model.get_transform();
  const positions = object.get_positions();
  const tex_coords = object.get_tex_coords();
  const vertices_count = object.get_vertices_count();

  for (let index = 0; index < vertices_count; index += 2) {
    let pos = vec2.fromValues(positions[index], positions[index + 1]);

    vec2.transformMat4(pos, pos, model);

    out_vertices.push(pos[0], pos[1]);

    out_tex_coords.push(tex_coords[index], tex_coords[index + 1]);
  }
}

//*****************************************************************************

function pack_corners(gl, corner_sprite, resolution_size) {
  let vertices = [];
  let tex_coords = [];

  {
    corner_sprite.model.set_position(0, 0);
    corner_sprite.model.set_rotation(0);
    push_vertices(vertices, tex_coords, corner_sprite);
  }

  {
    corner_sprite.model.set_position(0, resolution_size[1]);
    corner_sprite.model.set_rotation(-90);
    push_vertices(vertices, tex_coords, corner_sprite);
  }

  {
    corner_sprite.model.set_position(resolution_size[0], resolution_size[1]);
    corner_sprite.model.set_rotation(-180);
    push_vertices(vertices, tex_coords, corner_sprite);
  }

  {
    corner_sprite.model.set_position(resolution_size[0], 0);
    corner_sprite.model.set_rotation(90);
    push_vertices(vertices, tex_coords, corner_sprite);
  }

  return new Render_vertex_pack(
    gl,
    vertices,
    tex_coords,
    vertices.length,
    gl.TRIANGLES
  );
}

//*****************************************************************************

function update_corners_pack(pack, corner_sprite, resolution_size) {
  let vertices = [];
  let tex_coords = [];

  {
    corner_sprite.model.set_position(0, 0);
    corner_sprite.model.set_rotation(0);
    push_vertices(vertices, tex_coords, corner_sprite);
  }

  {
    corner_sprite.model.set_position(0, resolution_size[1]);
    corner_sprite.model.set_rotation(-90);
    push_vertices(vertices, tex_coords, corner_sprite);
  }

  {
    corner_sprite.model.set_position(resolution_size[0], resolution_size[1]);
    corner_sprite.model.set_rotation(-180);
    push_vertices(vertices, tex_coords, corner_sprite);
  }

  {
    corner_sprite.model.set_position(resolution_size[0], 0);
    corner_sprite.model.set_rotation(90);
    push_vertices(vertices, tex_coords, corner_sprite);
  }

  let start_index = 0;

  pack.update(start_index, vertices.length, vertices, tex_coords);
}

//*****************************************************************************

let isG_is_loaded_settings = false;

//*****************************************************************************
//                                  MAIN
//*****************************************************************************

function main() {
  const canvas = document.querySelector("#glCanvas");
  canvas.width = window.innerWidth;
  canvas.height = window.innerHeight;

  const gl = init_WebGL(canvas);
  if (!gl) return;

  let flame_shader = new Shader(
    gl,
    vertex_shader_source,
    fragment_shader_source
  );

  let texture_shader = new Shader(
    gl,
    vertex_shader_source_2,
    fragment_shader_source_2
  );

  if (!flame_shader.get_status() || !texture_shader.get_status()) {
    error_print("Shaders are not compiled!");
    return;
  }

  let isMain_status = false;
  let resolution_size = [gl.canvas.width, gl.canvas.height];
  let screen_texture_size = resolution_size;

  let near_far = [-10, 10];
  let left = 0;
  let right = resolution_size[0];
  let top = resolution_size[1];
  let bottom = 0;

  let projection_matrix = create_orthographic_matrix(
    left,
    right,
    bottom,
    top,
    near_far[0],
    near_far[1]
  );

  // Load textures
  let texture_loader = new Texture_loader(gl);
  let textures = new Texture_manager();

  let noise_texture_size = null;

  textures.add(
    "noise texture",
    texture_loader.load_texture(
      fractal_brownian_motion_noise_texture_path,
      gl.REPEAT,
      gl.REPEAT,
      gl.LINEAR,
      gl.LINEAR,
      (texture, image) => {
        if (texture === null) {
          error_print("Noise texture are not created!");
          return;
        }

        log_print("Noise texture are loaded!");
        noise_texture_size = texture.get_size();

        setup_flame_shader(
          flame_shader,
          projection_matrix,
          texture.get_size(),
          shader_settings
        );

        isMain_status = textures.is_has("noise texture");
      }
    )
  );

  setup_texture_shader(
    texture_shader,
    projection_matrix,
    resolution_size,
    screen_texture_size
  );

  let noise_texture = textures.get("noise texture");

  const corner_sprite_size = shader_settings.flame_max_height * 2;
  const corner_sprite_radius =
    shader_settings.corner_radius === 0 ? 0.01 : shader_settings.corner_radius;
  const corner_sprite_segments = 120;
  const corner_sprite_begin_angle = to_radians(270);
  const corner_sprite_end_angle = to_radians(180);

  let screen_sprite = new Sprite(gl, resolution_size[0], resolution_size[1]);
  let corner_sprite = new Rect_with_hole(
    gl,
    corner_sprite_size,
    corner_sprite_size,
    corner_sprite_radius,
    corner_sprite_begin_angle,
    corner_sprite_end_angle,
    corner_sprite_segments
  );

  const border_sprite_height = shader_settings.flame_max_height * 1;
  const border_sprite_long_width =
    resolution_size[0] - 2 * border_sprite_height;
  const border_sprite_short_width =
    resolution_size[1] - 2 * border_sprite_height;

  let border_long_sprite = new Sprite(
    gl,
    border_sprite_long_width,
    border_sprite_height
  );

  let border_short_sprite = new Sprite(
    gl,
    border_sprite_short_width,
    border_sprite_height
  );

  textures.add(
    "border texture",
    create_texture(gl, screen_texture_size[0], screen_texture_size[1])
  );
  const border_texture = textures.get("border texture");
  const border_framebuffer = create_framebuffer(
    gl,
    border_texture.get_native_handle()
  );

  textures.add(
    "corner texture",
    create_texture(gl, screen_texture_size[0], screen_texture_size[1])
  );
  const corner_texture = textures.get("corner texture");
  const corner_framebuffer = create_framebuffer(
    gl,
    corner_texture.get_native_handle()
  );

  flame_height_controller.height = shader_settings.flame_height;
  flame_height_controller.speed = shader_settings.linear_flame_animation_speed;

  let corner_pack = pack_corners(gl, corner_sprite, resolution_size);

  let flame_height = shader_settings.flame_height;

  let last_time = 0;
  let dt = 0;
  let interval_fps = 1 / shader_settings.max_fps;
  let isNeed_render = false;

  const settings_handler = {
    set(target, property, value) {
      log_print(`Свойство "${property}" изменено с ${target[property]} 
                на ${value}`);
      target[property] = value;

      setup_flame_shader(
        flame_shader,
        projection_matrix,
        noise_texture_size,
        shader_settings
      );

      switch (property) {
        case "flame_max_height": {
          if (value < shader_settings.flame_height) {
            shader_settings.flame_max_height = shader_settings.flame_height;
          } else {
            const corner_size = [value * 2, value * 2];

            const long_size = [resolution_size[0] - corner_size[0], value];
            border_long_sprite.set_size(long_size[0], long_size[1]);

            const short_size = [resolution_size[1] - corner_size[1], value];
            border_short_sprite.set_size(short_size[0], short_size[1]);

            corner_sprite.set_size(corner_size[0], corner_size[1]);

            update_corners_pack(corner_pack, corner_sprite, resolution_size);
          }

          break;
        }
        case "corner_radius": {
          corner_sprite.set_radius(value);

          update_corners_pack(corner_pack, corner_sprite, resolution_size);

          break;
        }
        case "flame_height": {
          if (shader_settings.flame_height > shader_settings.flame_max_height) {
            shader_settings.flame_height = shader_settings.flame_max_height;
          }

          flame_height = shader_settings.flame_height;

          break;
        }
        case "max_fps": {
          interval_fps = 1 / value;

          break;
        }
        case "linear_flame_animation_speed": {
          flame_height_controller.speed = value;

          break;
        }
        // No default
      }

      return true;
    },
  };

  shader_settings = new Proxy(shader_settings, settings_handler);

  function render(time) {
    requestAnimationFrame(render);

    if (isG_is_loaded_settings) {
      setup_flame_shader(
        flame_shader,
        projection_matrix,
        noise_texture_size,
        shader_settings
      );

      const value = shader_settings.flame_max_height;
      const corner_size = [value * 2, value * 2];

      const long_size = [resolution_size[0] - corner_size[0], value];
      border_long_sprite.set_size(long_size[0], long_size[1]);

      const short_size = [resolution_size[1] - corner_size[1], value];
      border_short_sprite.set_size(short_size[0], short_size[1]);

      corner_sprite.set_size(corner_size[0], corner_size[1]);
      corner_sprite.set_radius(shader_settings.corner_radius);

      update_corners_pack(corner_pack, corner_sprite, resolution_size);

      isG_is_loaded_settings = false;
    }

    let temporary_time = to_seconds(time);

    dt = temporary_time - last_time;

    if (!isMain_status) return;

    if (dt >= interval_fps) {
      last_time = temporary_time;

      isNeed_render = true;
    }

    if (!isNeed_render) return;

    gl.viewport(0, 0, gl.canvas.width, gl.canvas.height);
    gl.clearColor(0, 0, 0, 0); // Устанавливаем черный фон

    if (shader_settings.is_listen_microphone) {
      flame_height = update_flame_height(dt, shader_settings, flame_height);
    }

    gl.bindFramebuffer(gl.FRAMEBUFFER, border_framebuffer);
    gl.clear(gl.COLOR_BUFFER_BIT);

    bind_texture_unit(gl, noise_texture, 0);

    flame_shader.set_uniform_float("iTime", temporary_time);
    flame_shader.set_uniform_int("is_reflected", shader_settings.is_reflected);
    let size = border_long_sprite.get_size();
    flame_shader.set_uniform_float("object_size", size);
    flame_shader.set_uniform_float("flame_width", size[0]);
    flame_shader.set_uniform_float("flame_height", flame_height);

    border_long_sprite.model.set_position(size[1], 0);
    border_long_sprite.model.set_rotation(0);
    border_long_sprite.draw(flame_shader);

    border_long_sprite.model.set_position(
      resolution_size[0] - size[1],
      resolution_size[1]
    );
    border_long_sprite.model.set_rotation(180);
    border_long_sprite.draw(flame_shader);

    size = border_short_sprite.get_size();
    flame_shader.set_uniform_float("object_size", size[0], size[1]);
    flame_shader.set_uniform_float("flame_width", size[0]);
    flame_shader.set_uniform_float("flame_height", flame_height);

    border_short_sprite.model.set_position(0, resolution_size[1] - size[1]);
    border_short_sprite.model.set_rotation(-90);
    border_short_sprite.draw(flame_shader);

    border_short_sprite.model.set_position(resolution_size[0], size[1]);
    border_short_sprite.model.set_rotation(90);
    border_short_sprite.draw(flame_shader);

    gl.bindFramebuffer(gl.FRAMEBUFFER, corner_framebuffer);
    gl.clear(gl.COLOR_BUFFER_BIT);

    bind_texture_unit(gl, noise_texture, 0);

    size = corner_sprite.get_size();
    flame_shader.set_uniform_float("object_size", size);
    flame_shader.set_uniform_float("flame_width", size[0] * 0.5);
    flame_shader.set_uniform_float("flame_height", flame_height * 2);

    corner_pack.draw(flame_shader);

    gl.bindFramebuffer(gl.FRAMEBUFFER, null);

    gl.clear(gl.COLOR_BUFFER_BIT);

    bind_texture_unit(gl, border_texture, 0);
    bind_texture_unit(gl, corner_texture, 1);

    screen_sprite.draw(texture_shader);

    fps = 1 / dt;

    if (shader_settings.is_show_fps) {
      log_print("FPS: ", fps.toFixed(0));
    }

    isNeed_render = false;
  }

  requestAnimationFrame(render);
}

//*****************************************************************************

main();

//*****************************************************************************

navigator.mediaDevices
  .getUserMedia({
    audio: true,
    video: false,
  })
  .then(stream => {
    const audioContext = new AudioContext();
    const analyser = audioContext.createAnalyser();
    const microphone = audioContext.createMediaStreamSource(stream);
    const scriptProcessor = audioContext.createScriptProcessor(2048, 1, 1);

    analyser.smoothingTimeConstant = 0.8;
    analyser.fftSize = 1024;

    microphone.connect(analyser);
    analyser.connect(scriptProcessor);
    scriptProcessor.connect(audioContext.destination);
    scriptProcessor.onaudioprocess = function () {
      const array = new Uint8Array(analyser.frequencyBinCount);
      analyser.getByteFrequencyData(array);
      const arraySum = array.reduce((a, value) => a + value, 0);
      const average = arraySum / array.length;

      if (shader_settings.is_listen_microphone) {
        flame_height_controller.height =
          average * shader_settings.microphone_booster;
        flame_height_controller.is_need_update = true;
      }
    };
  })
  .catch(error => {
    /* handle the error */
    console.error(error);
  });

//*****************************************************************************

function save_settings() {
  const jsonString = JSON.stringify(shader_settings, null, 2);

  const blob = new Blob([jsonString], { type: "application/json" });

  const url = URL.createObjectURL(blob);

  const a = document.createElement("a");
  a.href = url;
  a.download = "shader_settings.json";
  document.body.append(a);

  a.click();

  a.remove();
  URL.revokeObjectURL(url);
}

//*****************************************************************************

function load_settings() {
  const input = document.createElement("input");
  input.type = "file";
  input.accept = ".json";

  input.addEventListener("change", event => {
    const file = event.target.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.addEventListener("load", e => {
      try {
        const json = JSON.parse(e.target.result);
        shader_settings = { ...shader_settings, ...json };
        isG_is_loaded_settings = true;
        log_print("Настройки загружены:", shader_settings);
      } catch (error) {
        error_print("Ошибка при загрузке файла:", error);
        alert(
          "Ошибка: файл должен быть в формате JSON и содержать валидные настройки."
        );
      }
    });
    reader.readAsText(file);
  });

  input.click();
}
