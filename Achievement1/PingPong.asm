.nolist
.include "m328Pdef.inc"
.list

; http://gjlay.de/helferlein/avr-uart-rechner.html
.equ UART_SPEED = 103

.def temp = r16
.def overflows0 = r17
.def overflows2 = r18
.def temp_uart = r19
.def data = r20

.equ TIMER0_INTERVAL = 1    ; Таймер 0: срабатывает первым
.equ TIMER2_INTERVAL = 2    ; Таймер 2: срабатывает позже

; Получается, что минимальная сумма срабатывания таймеров, при условии, 
; что должно пройти не менее 4 тактов, равна 3
;
; Но можно сделать и меньше, если мы сделаем 
; .equ TIMER0_INTERVAL = 1 
; .equ TIMER2_INTERVAL = 1
; И ПРИ ЭТОМ СДЕЛАЕМ ТАК:
; НАЧАЛЬНОЕ СМЕЩЕНИЕ для гарантии 4 тактов разницы
;	ldi temp, 4             ; Смещаем начало счета таймера 2


.org 0x0000             
rjmp Reset  

.org 0x0012
rjmp timer2_overflow             
                                         
.org 0x0020              
rjmp timer0_overflow    

TIMER0_STR: 
    .db "ping", 13, 0

TIMER2_STR: 
    .db "pong", 13, 0


Reset: 

	; Инициализация стека
	ldi temp, high(RAMEND)
    out SPH, temp
    ldi temp, low(RAMEND)
    out SPL, temp

    call uart_init

	; === НАСТРОЙКА ТАЙМЕРА 0 ===
	ldi temp, 0b00000101	; предделитель 1024
	out TCCR0B, temp      
               
	ldi temp, 0b00000001	; Разрешить прерывание по переполнению
	sts TIMSK0, temp       
 
	clr temp
	out TCNT0, temp			; Timer0
	clr overflows0

	; === НАСТРОЙКА ТАЙМЕРА 2 ===
	ldi temp, 0b00000111	; предделитель 1024
	sts TCCR2B, temp

	ldi temp, 0b00000001	; Разрешить прерывание по переполнению
	sts TIMSK2, temp

	           
	sts TCNT2, temp			; Timer2
	clr overflows2

	sei

main:
    rjmp main

; === ОБРАБОТЧИК ТАЙМЕРА 0 ===
timer0_overflow: 
    push r21
    in r21, SREG
    push r21
    
    inc overflows0        
    cpi overflows0, TIMER0_INTERVAL    
    brne timer0_end       
             
    clr overflows0 
    rcall send_message1
    
    ; Отключаем таймер 0 после срабатывания
    ldi r21, 0b00000000
    sts TIMSK0, r21

timer0_end:
    pop r21
    out SREG, r21
    pop r21
    reti                  

; === ОБРАБОТЧИК ТАЙМЕРА 2 ===
timer2_overflow:
    push r22
    in r22, SREG
    push r22
    
    inc overflows2         
    cpi overflows2, TIMER2_INTERVAL    
    brne timer2_end       
            
    clr overflows2
    rcall send_message2

    ; Отключаем таймер 2 после срабатывания
    ldi r22, 0b00000000
    sts TIMSK2, r22

timer2_end:
    pop r22
    out SREG, r22
    pop r22
    reti

send_message1:
    push ZL
    push ZH
    push data
    push r23
    
	ldi ZL, low(TIMER0_STR*2)
    ldi ZH, high(TIMER0_STR*2)
    rcall uart_print     
    
    pop r23
    pop data
    pop ZH
    pop ZL
	ret

send_message2:
    push ZL
    push ZH
    push data
    push r24
    
	ldi ZL, low(TIMER2_STR*2)
    ldi ZH, high(TIMER2_STR*2)
    rcall uart_print     
    
    pop r24
    pop data
    pop ZH
    pop ZL
	ret

uart_init:
    ldi temp_uart, high(UART_SPEED)      
    sts UBRR0H, temp_uart
    ldi temp, low(UART_SPEED)
    sts UBRR0L, temp_uart
    
    ldi temp_uart, (1<<TXEN0)
    sts UCSR0B, temp_uart
    
    ldi temp_uart, (1<<UCSZ01)|(1<<UCSZ00)
    sts UCSR0C, temp_uart
    ret

uart_print:
    lpm data, Z+
    cpi data, 0
    breq uart_print_done
    rcall uart_transmit
    rjmp uart_print
uart_print_done:
    ret

uart_transmit:
    ; Ждем пустой буфер
    lds temp, UCSR0A
    sbrs temp, UDRE0
    rjmp uart_transmit
    
    ; Кладем данные в буфер
    sts UDR0, data
    ret
